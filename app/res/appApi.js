/**
 * CRM App API SDK
 * จัดการการเชื่อมต่อ API, การเข้ารหัส Payload (AES/RSA), Session ของผู้ใช้งาน และการอัปโหลดไฟล์
 * Requires: CryptoJS, Node-Forge (ต้อง import ไว้ใน HTML ก่อนเรียกใช้ไฟล์นี้)
 */

(function (global) {
    'use strict';

    // ==========================================
    // 1. STATE MANAGEMENT (เก็บข้อมูลส่วนกลาง)
    // ==========================================
    const state = {
        config: {
            URL: '',
            AppId: '',
            ApiKey: '',
            GameBundle: '',
            GameConfigId: ''
        },
        openApi: {
            GameKey: '',
            GameSecret: ''
        },
        sessionToken: null,
        xmlRsaKey: null,
        userData: null,    // ข้อมูล User หลัง Login สำเร็จ
        gameConfig: null   // ข้อมูล GameConfig หลังดึงสำเร็จ
    };



    // ทำให้ข้างนอกสามารถเข้าถึง state ได้ผ่าน window.appData
    global.appData = state;

    // ==========================================
    // 2. CRYPTO & UTILITY FUNCTIONS (ฟังก์ชันเข้ารหัส)
    // ==========================================
    const CryptoUtils = {
        // สร้าง 32-bit integer hash จาก String
        generateHash: function (input) {
            let hash = 0;
            if (input.length === 0) return hash;
            for (let i = 0; i < input.length; i++) {
                let charCode = input.charCodeAt(i);
                hash = ((hash << 5) - hash) + charCode;
                hash |= 0;
            }
            return Math.abs(hash);
        },

        // สร้าง Fake Hash สำหรับหลอกระบบ
        generateFakeHash: function (input) {
            const hash = this.generateHash(input);
            const r1 = Math.floor(Math.random() * (99999999 - 11111111 + 1)) + 11111111;
            const r2 = Math.floor(Math.random() * (999999 - 111111 + 1)) + 111111;
            return `F${r1}${hash}${r2}`;
        },

        // ดึงอักขระแบบสลับช่อง
        generateHalfFk: function (fk) {
            let result = "";
            let isEven = false;
            for (let i = 0; i < fk.length; i++) {
                if (isEven) result += fk[i];
                isEven = !isEven;
            }
            return result;
        },

        // ถอดรหัส Simple Hash (ใช้กับ Hash ของ GameConfig)
        simpleHashDecrypt: function (cipherTextBase64, key) {
            const binaryString = atob(cipherTextBase64);
            const data = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) data[i] = binaryString.charCodeAt(i);
            const encoder = new TextEncoder();
            const keyBytes = encoder.encode(key);
            for (let i = 0; i < data.length; i++) data[i] ^= keyBytes[i % keyBytes.length];
            const decoder = new TextDecoder('utf-8');
            return decoder.decode(data);
        },

        // เข้ารหัส AES (CBC/PKCS7)
        aesEncrypt: function (plainText, password) {
            if (!global.CryptoJS) throw new Error("CryptoJS is missing!");
            const md5Hex = CryptoJS.MD5(password).toString(CryptoJS.enc.Hex);
            const key = CryptoJS.enc.Utf8.parse(md5Hex);
            const iv = CryptoJS.lib.WordArray.random(16);
            const encrypted = CryptoJS.AES.encrypt(plainText, key, {
                iv: iv,
                mode: CryptoJS.mode.CBC,
                padding: CryptoJS.pad.Pkcs7
            });
            return {
                IV: CryptoJS.enc.Base64.stringify(iv),
                EncryptedText: encrypted.toString()
            };
        },

        // เข้ารหัส RSA แบบ OAEP
        rsaEncryptOAEP: function (text, xmlKey) {
            if (!global.forge) throw new Error("Node-forge is missing!");
            const parser = new DOMParser();
            const xmlDoc = parser.parseFromString(xmlKey, "text/xml");
            const b64Modulus = xmlDoc.getElementsByTagName("Modulus")[0].childNodes[0].nodeValue;
            const b64Exponent = xmlDoc.getElementsByTagName("Exponent")[0].childNodes[0].nodeValue;
            const n = new forge.jsbn.BigInteger(forge.util.bytesToHex(forge.util.decode64(b64Modulus)), 16);
            const e = new forge.jsbn.BigInteger(forge.util.bytesToHex(forge.util.decode64(b64Exponent)), 16);
            const publicKey = forge.pki.setRsaPublicKey(n, e);
            const encryptedBytes = publicKey.encrypt(text, 'RSA-OAEP');
            return forge.util.encode64(encryptedBytes);
        }
    };

    // ==========================================
    // 3. CORE API LOGIC (แกนกลางการยิง API)
    // ==========================================
    /**
     * @typedef {Object} ResponseData
     * @property {Object} result
     * @property {number} result.code
     * @property {string} result.message
     * @property {any} result.data
     */
    const CoreApi = {
        // ฟังก์ชันพื้นฐานสำหรับยิง Request
        fetchApi: async function (functionName, formBody) {
            if (!state.config.URL) throw new Error("AppApi is not initialized! Call AppApi.init() first.");

            const endpoint = `${state.config.URL}/functions/${functionName}`;
            const headers = {
                "X-Parse-Application-Id": state.config.AppId,
                "X-Parse-REST-API-Key": state.config.ApiKey,
                "Content-Type": "application/json"
            };

            if (state.sessionToken) {
                headers["X-Parse-Session-Token"] = state.sessionToken;
            }

            try {
                const response = await fetch(endpoint, {
                    method: 'POST',
                    headers: headers,
                    body: JSON.stringify(formBody)
                });
                return await response.json();
            } catch (error) {
                console.error(`[API Error] /${functionName}:`, error);
                throw error;
            }
        },

        // ยิง API ตรงๆ แบบไม่ห่อ Payload (สำหรับ Login / ValidateSession และกลุ่มไม่มี Encryption)
        requestDirect: async function (functionName, body) {
            return await this.fetchApi(functionName, body);
        },

        // ยิง API แบบห่อ Payload และเข้ารหัส (ตามโครงสร้าง ParseAPI.cs)
        requestEncrypted: async function (functionName, payload, extraBody = {}) {
            if (!state.sessionToken || !state.xmlRsaKey) {
                throw new Error(`Cannot call ${functionName}: SessionToken or RSA Key is missing. Please login first.`);
            }

            // บังคับแนบ GameBundle เข้าไปใน Payload เสมอ
            payload.gameBundle = state.config.GameBundle;
            const payloadString = JSON.stringify(payload);

            // 1. คำนวณ Key
            const fk = CryptoUtils.generateFakeHash(payloadString);
            const halfFk = CryptoUtils.generateHalfFk(fk);
            const preFinalKey = `${state.sessionToken}_${halfFk}`;
            const finalKey = `${CryptoUtils.generateHash(preFinalKey)}`;

            // 2. เข้ารหัส
            const encryptedAES = CryptoUtils.aesEncrypt(payloadString, finalKey);
            const encryptedFkRSA = CryptoUtils.rsaEncryptOAEP(fk, state.xmlRsaKey);
            const encryptedIvRSA = CryptoUtils.rsaEncryptOAEP(encryptedAES.IV, state.xmlRsaKey);

            // 3. สร้าง Form (เอา extraBody มาเป็นฐาน แล้วเสริมตัวเข้ารหัสเข้าไป)
            const form = Object.assign({}, extraBody, {
                payload: encryptedAES.EncryptedText,
                fk: encryptedFkRSA,
                cache: encryptedIvRSA
            });

            return await this.fetchApi(functionName, form);
        }
    };

    // ==========================================
    // 4. PUBLIC API EXPORT (ฟังก์ชันสำหรับให้ภายนอกเรียกใช้)
    // ==========================================
    global.AppApi = {

        /**
         * ตั้งค่าเริ่มต้นก่อนใช้งาน API
         * @param {Object} config - { URL, AppId, ApiKey, GameBundle, GameConfigId }
         */
        init: function (config) {
            if (config.URL) state.config.URL = config.URL.replace(/\/$/, ""); // ลบ slash ตัวสุดท้ายทิ้ง
            if (config.AppId) state.config.AppId = config.AppId;
            if (config.ApiKey) state.config.ApiKey = config.ApiKey;
            if (config.GameBundle) state.config.GameBundle = config.GameBundle;
            if (config.GameConfigId) state.config.GameConfigId = config.GameConfigId;
            console.log("🚀 AppApi Initialized:", state.config);
        },

        /** ดึง State ปัจจุบันทั้งหมด */
        getState: function () {
            return state;
        },

        /** ออกจากระบบ ล้าง Session */
        logout: function () {
            state.sessionToken = null;
            state.xmlRsaKey = null;
            state.userData = null;
            state.gameConfig = null;
        },

        // ==========================================
        // Auth APIs
        // ==========================================
        login: async function (uuid) {
            const res = await this.callApi('loginCustom', {
                gameConfigId: state.config.GameConfigId,
                uuid: uuid,
                data: { device: { platform: "web" } }
            });
            this._handleLoginResponse(res);
            return res;
        },

        validateSession: async function (jwtTokenString) {
            try {
                const base64 = jwtTokenString.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
                const jwtParams = JSON.parse(decodeURIComponent(atob(base64).split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join('')));

                if (jwtParams.gameConfigId) state.config.GameConfigId = jwtParams.gameConfigId;
                if (jwtParams.userToken) state.sessionToken = jwtParams.userToken;

                const res = await this.callApi('validateSession', {
                    gameToken: jwtParams.gameToken,
                    data: { device: { platform: "web" } }
                });
                this._handleLoginResponse(res);
                return res;
            } catch (err) {
                console.error("JWT Decode Error", err);
                throw new Error("Invalid JWT Token");
            }
        },

        _handleLoginResponse: function (res) {
            if (res && res.result && res.result.code === 200 && res.result.data) {
                state.sessionToken = res.result.data.user.sessionToken;
                state.xmlRsaKey = res.result.data.publicKey;
                state.userData = res.result.data;
            }
        },

        // ==========================================
        // Data & Profile APIs
        // ==========================================
        getInitConfig: async function () {
            return await this.callApi('getInitConfig', {
                gameConfigId: state.config.GameConfigId
            });
        },

        getGameStat: async function () {
            const res = await this.callApi('getGameStat', {
                gameConfigId: state.config.GameConfigId
            });
            if (res && res.result && res.result.code === 200 && res.result.data) {
                if (state.userData) state.userData.stat = res.result.data.stat;
            }
            return res;
        },

        getGameConfig: async function () {
            const res = await this.callApiInternal('getGameConfig', {});
            if (res && res.result && res.result.code === 200 && res.result.data) {
                let configData = res.result.data;
                if (configData.hash) {
                    const decryptedStr = CryptoUtils.simpleHashDecrypt(configData.hash, state.config.GameConfigId);
                    configData = JSON.parse(decryptedStr);
                }
                state.gameConfig = configData;
                return configData;
            }
            return res;
        },

        changeDisplayName: async function (newName) {
            return await this.callApiInternal('changeDisplayName', { displayName: newName });
        },

        // ==========================================
        // Custom Properties APIs
        // ==========================================
        /**
         * @param {string} tableName - 'profile', 'customProperties', หรือ 'daily'
         * @param {Array} opList - โครงสร้าง [{ op: 'update'|'increment'|'delete', key: '...', value: ... }]
         */
        updateTable: async function (tableName, opList = []) {
            return await this.callApiInternal('updateDict', {
                fieldName: tableName,
                opList: opList
            });
        },

        updateShare: async function (statId, opList = []) {
            return await this.callApiInternal('updateShare', {
                statId: statId,
                opList: opList
            });
        },

        // ==========================================
        // Leaderboard APIs
        // ==========================================
        getLeaderboard: async function (sortBy = "score", order = "descending", period = "allTime", count = 100) {
            return await this.callApiInternal('getLeaderboard', {
                sortBy, order, period, count
            });
        },

        getDiscovery: async function (userCount) {
            return await this.callApiInternal('getDiscovery', {
                userCount: userCount
            });
        },

        // ==========================================
        // Purchase & Catalog APIs
        // ==========================================
        /**
         * Helper สำหรับแปลง Object ธรรมดาให้เป็นโครงสร้าง PurchaseItemData
         * เช่น { "coin": 100, "gem": 5 } -> [{itemId: "coin", amount: 100}, {itemId: "gem", amount: 5}]
         */
        _formatPurchaseItems: function (itemsObj) {
            if (!itemsObj) return [];
            return Object.keys(itemsObj).map(key => ({
                itemId: key,
                amount: itemsObj[key]
            }));
        },

        purchaseItem: async function (itemsMap, opListCustomProperties = []) {
            const items = this._formatPurchaseItems(itemsMap);
            return await this.callApiInternal('purchaseItem', { items, opListCustomProperties });
        },

        purchaseCustomProperties: async function (itemsMap, opListCustomProperties = []) {
            const items = this._formatPurchaseItems(itemsMap);
            return await this.callApiInternal('purchaseCustomProperties', { items, opListCustomProperties });
        },

        consumeItem: async function (itemsMap, opListCustomProperties = []) {
            const items = this._formatPurchaseItems(itemsMap);
            return await this.callApiInternal('consumeItem', { items, opListCustomProperties });
        },

        // ==========================================
        // Gacha APIs
        // ==========================================
        openGacha: async function (gachaId, code = "") {
            return await this.callApiInternal('openGacha', { gachaId, code });
        },

        getGachaRate: async function (gachaId) {
            return await this.callApiInternal('getGachaRate', { gachaId });
        },

        // ==========================================
        // Task APIs
        // ==========================================
        createTask: async function (taskTypes = []) {
            return await this.callApiInternal('createTask', { types: taskTypes });
        },

        removeTask: async function (taskIds = []) {
            return await this.callApiInternal('removeTask', { ids: taskIds });
        },

        clearTask: async function () {
            return await this.callApiInternal('clearTask', {});
        },

        submitTask: async function (taskId, opListCustomProperties = []) {
            return await this.callApiInternal('submitTask', {
                id: taskId,
                opListCustomProperties
            });
        },

        submitAndCreateTasks: async function (submitTaskIds = [], createTaskTypes = [], opListCustomProperties = []) {
            return await this.callApiInternal('submitAndCreateTasks', {
                submitTaskIds: submitTaskIds,
                createTaskTypes: createTaskTypes,
                opListCustomProperties: opListCustomProperties
            });
        },

        // ==========================================
        // Session / Gameplay APIs
        // ==========================================
        createSession: async function (platform = "web") {
            const extraBody = { data: { device: { platform: platform } } };
            return await this.callApiInternal('createSession', {}, extraBody);
        },

        startSession: async function () {
            return await this.callApiInternal('startSession', {});
        },

        endSession: async function (score, durationInSeconds, isWin = true, rewardId = null, customProperties = null, customData = null) {
            const payload = {
                score: score,
                duration: durationInSeconds,
                isWin: isWin
            };
            if (rewardId) payload.rewardId = rewardId;
            if (customProperties) payload.customProperties = customProperties;
            if (customData) payload.custom = customData;

            return await this.callApiInternal('endSession', payload);
        },

        // ==========================================
        // Notification & TBS APIs (Public APIs - No AES Payload)
        // ==========================================
        otpRequest: async function (phone) {
            return await this.callApi('otpRequest', {
                gameConfigId: state.config.GameConfigId,
                phone: phone
            });
        },

        otpVerify: async function (pin, token) {
            return await this.callApi('otpVerify', {
                gameConfigId: state.config.GameConfigId,
                pin: pin,
                token: token
            });
        },

        submitProfile: async function (profileData) {
            const payload = Object.assign({ gameConfigId: state.config.GameConfigId }, profileData);
            return await this.callApi('submitProfile', payload);
        },

        sendSMS: async function (phone, message) {
            return await this.callApi('sendSMS', {
                gameConfigId: state.config.GameConfigId,
                msisdn: phone,
                message: message
            });
        },

        sendEmail: async function (email, subject = null, template_uuid = null, payload = null) {
            const body = {
                gameConfigId: state.config.GameConfigId,
                email: email
            };
            if (subject) body.subject = subject;
            if (template_uuid) body.template_uuid = template_uuid;
            if (payload) body.payload = payload;

            return await this.callApi('sendEmail', body);
        },


        // --- Core Helpers ---
        /**  @returns {Promise<ResponseData>} */
        callApiInternal: async function (functionName, payload, extraBody = {}) {
            return await CoreApi.requestEncrypted(functionName, payload, extraBody);
        },
        /**  @returns {Promise<ResponseData>} */
        callApi: async function (functionName, payload) {
            return await CoreApi.requestDirect(functionName, payload);
        },

        // ==========================================
        // Open & Upload APIs
        // ==========================================
        setupOpenApiKey: function (gameKey, gameSecret) {
            state.openApi.GameKey = gameKey;
            state.openApi.GameSecret = gameSecret;
            console.log("🔐 OpenApi Keys Configured.");
        },

        callApiOpen: async function (functionName, payload, callback = null) {
            try {
                if (!state.config.URL) throw new Error("AppApi is not initialized!");

                const response = await fetch(`${state.config.URL}/open/${functionName}`, {
                    method: 'POST',
                    headers: {
                        'GameKey': state.openApi.GameKey,
                        'GameSecret': state.openApi.GameSecret,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(payload || {})
                });

                const res = await response.json();
                if (callback) callback(res);
                return res;

            } catch (error) {
                console.error(`[OnApiOpen Error] ${functionName}:`, error);
                const errRes = { result: { code: 500, message: error.message || "Network Error" } };
                if (callback) callback(errRes);
                return errRes;
            }
        },
        // ==========================================
        // Open & Upload APIs
        // ==========================================
        callApiUpload: async function (file, params) {
            const { codeName, action = null, subPath = null, extension = null } = params;

            try {
                if (!state.config.URL) throw new Error("AppApi is not initialized!");

                const formData = new FormData();
                const gameId = state.config.GameConfigId || "";
                const fileExt = extension || (file.name ? "." + file.name.split('.').pop() : "");

                formData.append('gameId', gameId);
                formData.append('codeName', codeName);


                if (file) formData.append('file', file);
                if (action) formData.append('action', action);
                if (subPath) formData.append('subPath', subPath);
                if (extension) formData.append('extension', fileExt);

                const headers = {};
                if (state.sessionToken) headers['X-Parse-Session-Token'] = state.sessionToken;
                if (state.openApi.GameKey) headers['X-Parse-Session-Token'] = state.openApi.GameKey;
                if (state.openApi.GameSecret) headers['X-Parse-Session-Token'] = state.openApi.GameSecret;

                const response = await fetch(`${state.config.URL}/upload`, {
                    method: 'POST',
                    headers: headers,
                    body: formData
                });

                const res = await response.json();
                if (callback) callback(res);
                return res;

            } catch (error) {
                console.error(`[OnApiUpload Error]:`, error);
                const errRes = { result: { code: 500, message: error.message || "Upload Error" } };
                if (callback) callback(errRes);
                return errRes;
            }
        }

    };

})(window);