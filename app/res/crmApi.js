/**
 * CRM API (Service Layer)
 * เก็บเฉพาะ Logic, Data State และ API ของฝั่ง CRM
 */
(function (global) {
    'use strict';

    if (!global.AppApi) {
        console.error("[CrmApi] Require AppApi!");
        return;
    }

    /**
     * @typedef {Object} CurrentData
     * @property {string} language
     * @property {LiffPayload|null} LIFF_PAYLOAD
     * @property {InitializeData|null} initializeData
     * @property {StoreInfo|null} storeInfo
     * @property {string|null} gameId
     * @property {string|null} statId
     * @property {string|null} refId
     * @property {string|null} crmUserId
     * @property {string|null} lineUserId
     * @property {UserData|null} user
     * @property {StatData|null} stat
     * @property {CRMUserData|null} crmUser
     * @property {number} serverTime
     * @property {string|null} publicKey
     * @property {DailyData|null} daily
     * @property {MarketData|null} marketData
     * @property {MarketReward[]|null} rewards
     * @property {CouponData[]|null} coupons
     * @property {ShippingOrderData[]|null} orders
     * @property {LeaderboardData|null} leaderboard
     * @property {GameCampaign|null} gameCampaign
     */
    /** @type {CurrentData} */
    const currentData = {
        language: "En",
        LIFF_PAYLOAD: null,
        initializeData: null,
        storeInfo: null,
        gameId: null,
        statId: null,
        refId: null,
        crmUserId: null,
        lineUserId: null,
        user: null,
        stat: null,
        crmUser: null,
        serverTime: 0,
        publicKey: null,
        daily: null,
        marketData: null,
        rewards: null,
        coupons: null,
        orders: null,
        leaderboard: null,
        gameCampaign: null
    };



    let GameId;
    let StatId;


    // เปิดให้ข้างนอกอ่านค่าได้
    global.crmData = currentData;

    // 2. ฟังก์ชันหลักของ CRM
    global.CrmApi = {
        init: async function () {
            console.log("[CrmApi] ⚙️ Initializing CRM...");
            GameId = appData.config.GameConfigId;
            currentData.gameId = GameId;
            console.log("[CrmApi] GameId: " + GameId);
            console.log("[CrmApi] ✅ CRM Ready.");

            var STR_LIFF_PAYLOAD = sessionStorage.getItem("LIFF_PAYLOAD");
            if (STR_LIFF_PAYLOAD) {
                currentData.LIFF_PAYLOAD = JSON.parse(STR_LIFF_PAYLOAD);
                currentData.storeInfo = currentData.LIFF_PAYLOAD.storeInfo;
                currentData.initializeData = currentData.LIFF_PAYLOAD.initializeData;
            }

            Utility.awake();

            //-------------------------    
            //เข้าสู่ระบบด้วย accessToken จากการดึง quary Params
            var urlParams = new URLSearchParams(window.location.search);
            var accessToken = urlParams.get('accessToken');
            var themeId = urlParams.get('themeId');
            if (accessToken && themeId) {
                const res = await this.loginAccessToken(accessToken);
                await this.loginDone(res);
                return;
            }

            //เข้าสู่ระบบด้วย demo ทดสอบ login ตรงสำหรับ devtools
            var uid = urlParams.get('uid');
            if (uid) {
                const res = await this.loginTest(uid);
                await this.loginDone(res);
                return;
            }

            //-------------------------
            //เข้าสู่ระบบด้วย ใช้ค่ามารตราฐานจาก login
            if (currentData.LIFF_PAYLOAD) {
                const idToken = currentData.LIFF_PAYLOAD.idToken;
                const lineUserId = currentData.LIFF_PAYLOAD.userId;
                const res = await this.login(lineUserId, idToken, null);
                await this.loginDone(res);
                return;
            }


        },


        //** Auth **//
        login: async function (uid, idToken, phone) {
            /** @type { ResponseData } */
            const res = await AppApi.callApi('crm_auth', {
                gameId: GameId,
                uid: uid,
                idToken: idToken,
                phone: phone
            });
            return res;
        },
        loginAccessToken: async function (accessToken) {
            /** @type { ResponseData } */
            const res = await AppApi.callApi('crm_tokenSign', {
                gameId: GameId,
                accessToken: accessToken
            });
            return res;
        },
        loginTest: async function (uid) {
            /** @type { ResponseData } */
            const res = await AppApi.callApi('crm_login_editor', {
                gameId: GameId,
                uid: uid,
                EDITORKEY: appConfig.EDITORKEY
            });
            return res;
        },
        /** @param {ResponseData} res */
        loginDone: async function (res) {
            // INVALID, 
            // OPEN_REGISTER , OPEN_OTP_PIN, OPEN_LOGIN
            // LOGIN_DONE , 
            // VERIFY_DONE
            // EXPIRE
            console.log("[CrmApi] loginDone:", res.result.code);
            if (res.result.code == 200) {
                const action = res.result.data.action;
                console.log("action :" + action);
                if (action == "LOGIN_DONE") {
                    // continue

                    const result = res.result.data;
                    StatId = result.stat.statId;
                    currentData.statId = result.stat.statId;
                    currentData.refId = result.stat.refId;
                    currentData.crmUserId = result.crmUser.user.id;
                    currentData.lineUserId = result.crmUser.user.lineUserId;
                    currentData.stat = result.stat;
                    currentData.user = result.user;
                    currentData.crmUser = result.crmUser;
                    currentData.serverTime = result.serverTime;
                    currentData.publicKey = result.publicKey;
                    currentData.daily = result.dailyData;
                    console.log(JSON.stringify(currentData));

                    AppApi._handleLoginResponse(res);
                    await this.getMarketData();
                    await Utility.startApp();

                }
                else if (action == "EXPIRE" || action == "INVALID") {

                    // redirect to login page
                    console.log("[LOGIN] invalid");
                    Utility.confirmRedirectTologin(action);
                }
                else {
                    // open login panel...
                    console.log("[LOGIN] open login panel");
                    PageManager.loadLoginScreen();
                }
            }
            else {
                this.onfailed(res);
            }
        },


        //  ** OTP ** //
        onOTPVerify: async function (verificationToken, pin) {
            /** @type { ResponseData } */
            const res = await AppApi.callApi('crm_verify', {
                gameId: GameId,
                verificationToken: verificationToken,
                pin: pin
            });
            if (res.result.code == 200) {
                const action = res.result.data.action;
                return action;
            }
            else {
                return null;
            }
        },


        //** Get Info **/
        getInfo: async function () {
            /** @type { ResponseData } */
            const res = await AppApi.callApi('crm_getpublicinfo', { gameId: GameId });
            if (res.result.code == 200) {
                currentData.publicInfo = res.result.data;
                return currentData.publicInfo;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },


        //** Profile **/
        getProfile: async function () {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getuser', {});
            if (res.result.code == 200) {
                currentData.crmUser = res.result.data.crmUser;
                return currentData.crmUser;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },
        updateProfile: async function (profile) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_updateProfile', { profile: profile });
            if (res.result.code == 200) {
                console.log("OK upload done");
                currentData.crmUser = res.result.data.crmUser;
                return currentData.crmUser;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },
        updateProfileCustomFields: async function (customFields) {
            /** @type { ResponseData } */
            const profile = {
                [TYPES.profileFields.customFields]: customFields
            }
            return await this.updateProfile(profile);
        },


        searchAddress: async function (province, amphoe, district) {

            // `?province=${encodeURIComponent(province)}&amphoe=${encodeURIComponent(amphoe)}&district=${encodeURIComponent(district)}`
            // `?province=${encodeURIComponent(province)}&amphoe=${encodeURIComponent(amphoe)}&district=${encodeURIComponent(district)}`
            let path = "getAddress";
            if (province) path += `?province=${encodeURIComponent(province)}`;
            if (amphoe) path += `&amphoe=${encodeURIComponent(amphoe)}`;
            if (district) path += `&district=${encodeURIComponent(district)}`;

            /** @type { ResponseData } */
            const res = await AppApi.callApiOpen(path, {});
            console.log(res);
            if (!res) return [];
            return res;
        },


        //** Address **/
        getAddress: async function () {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getAddresses', {});
            if (res.result.code == 200) {
                currentData.crmUser.userAddresses = res.result.data.address;
                return currentData.crmUser.userAddresses;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },
        /**  @param {CRMAddressData} address */
        createAddress: async function (address) {
            address.id = null;
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_createAddresses', address);
            if (res.result.code == 200) {
                return true;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },
        /** @param {CRMAddressData} address */
        updateAddress: async function (address) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_updateAddresses', address);
            if (res.result.code == 200) {
                return true;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },
        setDefaultAddress: async function (id) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_updateAddresses', { id: id, isDefault: true });
            if (res.result.code == 200) {
                return true;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },
        onRemoveAddress: async function (id) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_removeAddresses', { id: id });
            if (res.result.code == 200) {
                await this.getAddress();
                return true;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },



        //** Market Data **/
        getMarketData: async function () {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getMarketData', {});
            if (res.result.code == 200) {
                currentData.marketData = res.result.data;
                return currentData.marketData;
            }
            else {
                this.onfailed(res);
                return null;
            }

        },


        //** Reward **/
        getRewards: async function () {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getRewards', {});
            if (res.result.code == 200) {
                currentData.rewards = res.result.data.rewards;
                return currentData.rewards;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },

        getReward: async function (id) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getReward', { id: id });
            if (res.result.code == 200) {
                currentData.rewards[id] = res.result.data.reward;
                return currentData.rewards[id];
            }
            else {
                this.onfailed(res);
                return null;
            }
        },

        onRedeemReward: async function (rewardId, quantity, addressId) {
            /** @type { ResponseData } */
            const body = {
                rewardId: rewardId,
                quantity: quantity
            };
            if (addressId)
                body.addressId = addressId;
            const res = await AppApi.callApiInternal('crm_redeemReward', body);
            if (res.result.code == 200) {

                if (res.result.data.points)
                    currentData.crmUser.user.points = res.result.data.points;

                return true;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },


        //** Shipping Reward **/
        getMyShippingRewards: async function (limit = 20, offset = 0) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getShippingRewards', { limit: limit, offset: offset });
            if (res.result.code == 200) {
                return res.result.data;
                /*
                    "orders": [
                            {
                                id: number;
                                status: string; // processing,shipped,delivered 
                                trackingNumber: string;
                                createdAt: string;
                                updatedAt: string;
                                shippingAddress: UserAddressesData;
                                items: [
                                    {
                                        rewardId: number;
                                        name: string;
                                        description: string;
                                        image: string;
                                        points: number;
                                        quantity: number;
                                    }
                                ];
                            }
                        ],
                        "summary": {
                            "activeCount": 17,
                            "usedCount": 6,
                            "totalCount": 23
                        },
                        "pagination": {
                            "total": 23,
                            "limit": 20,
                            "offset": 0,
                            "hasMore": true
                        }
                */
            }
            else {
                this.onfailed(res);
                return null;
            }
        },

        //** Coupon **/
        getMyCoupon: async function (couponCode) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getMyCoupons', { couponCode: couponCode });
            if (res.result.code == 200) {
                return res.result.data;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },

        getMyCoupons: async function (limit = 20, offset = 0) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getMyCoupons', { limit: limit, offset: offset });
            if (res.result.code == 200) {
                currentData.coupons = res.result.data.coupons;
                return res.result.data;
                /*
                    "coupons": [
                            {
                                "id": 51,
                                "rewardId": 78,
                                "rewardName": "Lucky 20% Discount",
                                "rewardDescription": "Available today-31/12/2026",
                                "rewardImage": "https://assets.mookept.com/rewards/1774350969252-357c2d1e59af4e309f4ddb6119319d5a.png",
                                "rewardType": "digital",
                                "couponCode": "LUCKY-009",
                                "status": "active",
                                "codeStatus": "issued",
                                "notes": null,
                                "redeemedAt": "2026-05-14T17:50:58.000Z",
                                "usedAt": null
                            }
                        ],
                        "summary": {
                            "activeCount": 17,
                            "usedCount": 6,
                            "totalCount": 23
                        },
                        "pagination": {
                            "total": 23,
                            "limit": 20,
                            "offset": 0,
                            "hasMore": true
                        }
                */
            }
            else {
                this.onfailed(res);
                return null;
            }
        },


        //** Point **
        getEarnedPoints: async function (limit = 20, offset = 0) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getEarnedPoints', { limit: limit, offset: offset });
            if (res.result.code == 200) {

                return res.result.data.transactions;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },

        getDeductPoints: async function (limit = 20, offset = 0) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getDeductPoints', { limit: limit, offset: offset });
            if (res.result.code == 200) {

                return res.result.data.deduction_history;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },


        //** Leaderboard **
        getLeaderboard: async function (id) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getLeaderboard', { id: id });
            if (res.result.code == 200) {
                currentData.leaderboard = res.result.data.leaderboard;
                return currentData.leaderboard;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },


        //** Marketplace **
        linkmarketplace: async function (marketplace, order_id, zip_code) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_linkmarketplace', {
                marketplace,
                order_id,
                zip_code
            });
            if (res.result.code == 200) {
                return res.result.data.linkStatus;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },


        onGetOneTimeToken: async function () {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_getOnetimeToken', {});
            if (res.result.code == 200) {
                return res.result.data.token;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },


        //** Game **
        /** @returns {GotoGameData} */
        onGotoGame: async function (gameId) {
            /** @type { ResponseData } */
            const res = await AppApi.callApiInternal('crm_gotoGameCampaign', { gameId: gameId });
            if (res.result.code == 200) {
                return res.result.data;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },

        /** @returns {GachaData} */
        onOpenGacha: async function (gameId) {
            const prepair = { gameId: gameId };
            const res = await AppApi.callApiInternal('crm_prepairGacha', prepair);
            if (res.result.code == 200) {
                var token = res.result.data.token;
                if (token) {
                    var payload = {};
                    payload.gameId = gameId;
                    payload.token = token;
                    const res = await AppApi.callApiInternal('crm_openGacha', payload);
                    if (res && res.result.code == 200 && res.result.data.gachaResult) {
                        return res.result.data.gachaResult;
                    }
                    else {
                        //this.onfailed(res);
                        return null;
                    }
                }
            }
            else {
                //this.onfailed(res);
                return null;
            }
        },

        /** @returns {List<GachaData>} */
        onGetGachaRate: async function (gameId) {
            const res = await AppApi.callApiInternal('getGachaRate', { gachaId: gameId });
            if (res.result.code == 200) {
                return res.result.data.gachaRate;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },


        onCheckOCR: async function (jobId, ida) {
            let body = {
                gameId: GameId,
                statId: StatId,
            };
            if (jobId && ida) {
                body.jobId = jobId;
                body.ida = ida;
            }
            else {
                body.limit = 30;
                body.offset = 0;
            }
            const res = await AppApi.callApiInternal('crm_checkOCR', body);
            if (res.result.code == 200) {
                return res.result.data;
            }
            else {
                this.onfailed(res);
                return null;
            }
        },
        onUploadReceiptImage: async function (files) {

            const token = await this.onGetOneTimeToken();
            if (!token) return null;

            const formData = new FormData();
            formData.append('gameId', GameId);
            formData.append('statId', StatId);
            formData.append('action', 'crm_ocrupload');
            formData.append('token', token);

            if (files.length === 1) {
                formData.append('files', files[0]);
            } else {
                const pdfBlob = await generatePDF(files);
                formData.append('files', pdfBlob, 'multi_receipts.pdf');
            }

            const res = await AppApi.callApiUpload(files, null, formData);
            if (res.code == 200) {
                return res.data;
            }
            else {
                return null;
            }
        },



        /** @param {ResponseData} res */
        onfailed: function (res) {
            /*
            {
                "result": {
                    "code": 404,
                    "message": "NOTFOUND"
                }
            }
            */
            const code = res?.result?.code || "Unknown Error";
            const message = res?.result?.message || "Something went wrong.";

            // เรียกใช้งาน Popup Failed ผ่าน PageManager
            if (PageManager.showFailed) {
                PageManager.showFailed(`Error (${code})`, message, "❌");
            } else {
                console.error("[CrmApi] Failed:", code, message);
                alert(`Error (${code}): ${message}`);
            }
        }
    };


})(window);