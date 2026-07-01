
(function (global) {
    'use strict';

    const params = new URLSearchParams(window.location.search);

    global.TYPES = {
        marketplace: {
            shopee: 'shopee',
            lazada: 'lazada',
            tiktok: 'tiktok'
        },
        profileFields: {
            email: 'email',
            customFields: 'customFields',
            displayName: 'displayName',
            pictureUrl: 'pictureUrl'
        },
        customFields: {
            gender: 'gender',
            birthday: 'birthday',
            consent_notif: 'consentnotif'
        },
        gamifyFeatures: {
            dailyCheckin: 'dailyCheckin',
            referralCode: 'referralCode',
            qrHunt: 'qrHunt'
        },
        rewardTypes: {
            physical: 'physical',
            digital: 'digital',
            game: 'game'
        },
        gameRewardTypes: {
            item: 'item',
            currency: 'currency',
            _event: 'event',
            gacha: 'gacha'
        }
    };



    window.addEventListener('keydown', (e) => {
        // ดักจับ Event F2 เพื่อเปิดหน้า Debug
        if (e.key === 'F6') {
            e.preventDefault();
            if (PageManager) {
                PageManager.loadPage('debug');
            }
        }

        if (e.key === 'F4') {
            e.preventDefault();
            sessionStorage.setItem('VERSION', Date.now());
            location.reload();
        }
    });



    // 2. ฟังก์ชันหลักของ CRM
    global.Utility = {


        awake: async function () {
            //Tittle
            document.title = crmData.storeInfo.storeName;
        },

        getDomainOrSubdomain: function () {
            const host = window.location.hostname.toLowerCase();

            if (host.includes("reflex")) {
                // https://nike.reflex-crm.co
                const parts = host.split(".");
                if (host.includes("localhost") || parts.length < 3) {
                    return null;
                }
                return parts[0]; // subdomain เช่น nike
            }
            else {
                return window.location.origin; // https://crm.nike.com
            }
        },

        checkAppId: async function () {



            console.log("Utility.checkAppId");
            //return "crm_platform";

            //** 1.มี accessToken เส้นใหญ่ไฟกระพริบ เข้าไปเลย ไม่ต้องเช็คอะไรแล้ว (สำหรับ DEV เทสเอง) */
            const accessToken = params.get('accessToken');
            const uid = params.get('uid');
            if (accessToken) {
                return params.get('appid'); // ถ้ามาอย่างถูกต้อง มันจะมี appid มาเอง *ถ้าไม่มีก็ไม่ต้องสนใจ..  ปล่อยมันพัง มาแปลก?
            }

            //** 2.ดึง subdomain และ appid จาก url หน้านี้*/
            const mysubDomain = this.getDomainOrSubdomain();
            const myappid = params.get('appid');
            if (!myappid && !mysubDomain) {
                //** ไม่มีอะไรสักอย่าง ถีบออกไปหน้า landing page.... */
                window.location.href = "https://reflexstudio.co/";
                return null;
            }

            //** 3.ดึง subdomain และ appid จากหน้า linelogin */
            let appOk = true;
            let pageContent = null;
            const appId = sessionStorage.getItem('CRM_APPID');
            const subDomain = sessionStorage.getItem('CRM_SUBDOMAIN');

            //** 4.ไม่มี  appId จบตรงนี้เลย.... กลับไป login ใหม่*/
            if (!appId) {
                appOk = false;
                console.log("[CHECKAPPID] not has appid");
                pageContent = "NOT HAS APPID";
            }
            //** 5.มี subDomain แต่ไม่ตรงกัน มาไง? ไม่ผ่าน..ถีบไป login */
            if (mysubDomain && subDomain && mysubDomain !== subDomain) {
                appOk = false;
                console.log("[CHECKAPPID] not has subdomain");
                pageContent = `NOT HAS SUBDOMAIN ${mysubDomain} != ${subDomain}`;
            }
            //** 6.มี appid มาครบ ทั้งหน้านี้และได้จาก login แต่ดันไม่ตรงกัน.. กลับไป login ใหม่ */
            if (myappid && appId && myappid !== appId) {
                appOk = false;
                console.log("[CHECKAPPID] not match appid");
                pageContent = "NOT MATCH APPID";
            }
            //** 7.ไม่มี payload สำหรับ store แปลกๆ... กลับไป login ใหม่ */
            if (!sessionStorage.getItem('LIFF_PAYLOAD')) {
                appOk = false;
                console.log("[CHECKAPPID] not has payload");
                pageContent = "NOT HAS PAYLOAD";
            }

            //** 8.มีครบทุกอย่าง แต่หมดเวลา.... กลับไป login ใหม่ */
            let now = Date.now();
            let lastLogin = parseInt(sessionStorage.getItem('LIFF_UNIX') || 0);
            let timeout = (lastLogin + 60000 * 60) < now; // 60 นาที.
            if (timeout && !uid) {
                appOk = false;
                console.log("[CHECKAPPID] timeout");
                pageContent = "TIMEOUT";
            }

            if (appOk) {
                console.log("[CHECKAPPID] success");
                return appId;
            }
            else {
                this.confirmRedirectTologin(pageContent);
                return null;
            }
        },

        startApp: async function () {


            if (crmData.user) {
                // first time popup!..
            }

            // เปิดหน้าแรก
            PageManager.console.visible(true);
            PageManager.loadPage('home');

            this.applyGlobalTheme();

        },

        // --- ระบบตั้งค่า Global Background & Theme ---
        applyGlobalTheme: function () {
            const root = document.documentElement;
            // ดึงข้อมูลการตั้งค่ามาจาก API หรือ Payload
            const initData = crmData?.marketData?.initializeData || crmData?.initializeData;

            if (initData) {
                // อัปเดตสีหลักและสีรอง (ถ้ามี)
                if (initData.primaryColor) {
                    root.style.setProperty('--theme-primary', initData.primaryColor);
                }
                if (initData.secondaryColor) {
                    root.style.setProperty('--theme-secondary', initData.secondaryColor);
                }

                // อัปเดตพื้นหลังของแอป (Solid Color หรือ Gradient)
                if (initData.bgColor) {
                    root.style.setProperty('--theme-bg-color', initData.bgColor);
                }

                // อัปเดตรูปภาพพื้นหลังของแอป (ถ้ามี)
                if (initData.customAssets && initData.customAssets.bgImg) {
                    root.style.setProperty('--theme-bg-image', `url('${initData.customAssets.bgImg}')`);
                } else {
                    root.style.setProperty('--theme-bg-image', 'none');
                }
            }
            console.log("[Utility] Global Theme Applied.");
        },

        getPoints: function () {
            var points = crmData?.crmUser?.user?.points || 0;
            return points;
        },

        isTierReady: function () {
            var tiers = crmData.marketData?.tiers || [];
            return tiers != null && tiers.length > 0;
        },
        getUserTier: function () {
            if (!this.isTierReady()) return null;
            var tier = crmData?.crmUser?.user?.tier || null;
            if (tier) {
                return this.getTier(tier.id);
            }
            else {
                var tiers = crmData.marketData?.tiers || [];
                return tiers.length > 0 ? tiers[0] : null;
            }
        },
        getUserNextTier: function () {
            if (!this.isTierReady()) return null;
            var tier = this.getUserTier();
            if (tier) {
                var tiers = crmData.marketData?.tiers || [];
                return tier == undefined ? tiers[0] : tiers.find(x => x.level > tier.level);
            }
            return null;
        },
        isMaxTier: function (tier) {
            if (!this.isTierReady()) return false;
            var tiers = crmData.marketData?.tiers || [];
            return tiers.find(x => x.level > tier.level) == null;
        },


        getAssetPath: function (id) {
            var assets = crmData.initializeData?.customAssets || {};
            if (assets.hasOwnProperty(id)) {
                console.log(`by server ${id}`);
                return assets[id];
            }
            if (appConfig.resPath.hasOwnProperty(id)) {
                console.log(`by default ${id} : ${appConfig.resPath[id]}`);
                return appConfig.resPath[id];
            }
            return null;
        },

        /** @returns {MarketReward} */
        getReward: function (id) {
            var rewards = crmData.marketData?.rewards || [];
            const reward = rewards.find(t => t.id == id);
            if (reward) {
                return reward;
            }
            return null;
        },

        /** @returns {MarketReward} */
        getRewardsForShop: function (types) {
            var allRewards = crmData.marketData?.rewards || [];
            const filteredRewards = allRewards.filter(t => types.includes(t.rewardType) && t.displayAtShop);
            return filteredRewards || [];
        },

        /** 
         * @param {MarketReward} reward 
         * @returns {boolean} */
        isEnough: function (reward, amount = 1) {
            return crmData.user.user.points >= (reward.points * amount);
        },

        /** 
         * @param {MarketReward} reward 
         * @returns {boolean} */
        isCanRedeem: function (reward) {
            var ok = reward.status == "active";
            if (!ok) return false;
            if (reward.canRedeem) {
                ok = reward.canRedeem;
                if (!ok) return false;
            }
            if (reward.tierRestricted) {
                ok = reward.tierEligible;
                if (!ok) return false;
            }
            if (reward.oneTimeRedemption) {
                if (reward.alreadyRedeemed)
                    return false;
            }
            return ok;
        },

        /** 
         * @param {MarketReward} reward 
         * @returns {boolean} */
        isGacha: function (reward) {
            if (reward.rewardType != TYPES.rewardTypes.game) return false;
            return reward.gameMetadata != null && reward.gameMetadata.type == TYPES.gameRewardTypes.gacha;
        },

        /** 
         * @param {MarketReward} reward 
         * @returns {GameCampaignData} */
        getRewardGameCampaign: function (reward) {
            var gameId = reward?.gameMetadata?.gameId;
            if (!gameId) return null;
            var gameCampaign = this.getGameCampaign(gameId);
            return gameCampaign;
        },

        /**
         * เปิด modal สำหรับแลกของรางวัล
         * @param {MarketReward} reward รหัสรางวัล
         * @param {function} callback ฟังก์ชันที่จะทำงานเมื่อปิด modal
         */
        openRedeem: function (reward, callback) {
            if (reward == null) return;
            if (this.isGacha(reward)) {
                var game = this.getRewardGameCampaign(reward);
                if (game) {
                    // OPEN GAME DETAIL PANEL...
                }
            }
            else {
                PageManager.openModal("redeemReward", { id: reward.id }, (res) => {
                    if (res && res.success) {
                        if (callback != null) {
                            callback();
                        }
                    }
                });
            }
        },


        /** @returns {TierData} */
        getTier: function (id) {
            var tiers = crmData.marketData?.tiers || [];
            const tier = tiers.find(t => t.id == id);
            if (tier) {
                return tier;
            }
            return null;
        },
        /** @returns {MarketBanner} */
        getBanner: function (id) {
            var marketBanners = crmData.marketData?.banners?.marketBanners || [];
            const banner = marketBanners.find(t => t.id == id);
            if (banner) {
                return banner;
            }
            return null;
        },
        /** @returns {GameCampaignData} */
        getGameCampaign: function (gameId) {
            var gameCampaigns = crmData.marketData?.gameCampaigns || [];
            const game = gameCampaigns.find(t => t.gameId == gameId);
            if (game) {
                return game;
            }
            return null;
        },

        getMarketplace: function (platforms) {
            const marketplace = {}
            platforms.forEach(platform => {
                const linked = crmData.user.statistics.orders.byMarketplace[marketplace] != null;
                const storeConnect = crmData.marketData.marketplaces.find(t => t.name === marketplace && t.connected) != null;
                if (storeConnect) {
                    marketplace[platform] = linked;
                }
            });
            return marketplace;
        },

        getGamifyFeatures: function (feature) {
            const marketData = crmData.marketData;
            if (feature === TYPES.gamifyFeatures.dailyCheckin) {
                return marketData.enableDailyCheckIn;
            }
            if (feature === TYPES.gamifyFeatures.referralCode) {
                return marketData.enableReferralCode;
            }
            if (feature === TYPES.gamifyFeatures.qrHunt) {
                return marketData.enableQRHunt;
            }
            return false;
        },



        confirmRedirectTologin: async function (because) {
            PageManager.showOk(
                "Something went wrong",
                "Action: '" + because + "'\nPlease re-login and try again",
                null,
                () => {
                    this.redirectTologin();
                });
        },
        redirectTologin: async function () {
            let redirectLogin = sessionStorage.getItem('CRM_REDIRECTLOGIN');
            if (!redirectLogin || redirectLogin == "undefined" || redirectLogin == "null" || redirectLogin == "") {
                console.error("CRM_REDIRECTLOGIN IS NULL");


                //** ดูจาก URL หน้าเว็ปเท่านั้นมี appid ไหม ถ้ามีกลับไปแบบมี ถ้าไม่มี กลับไปตรงๆได้เลย */
                const myappid = params.get('appid');
                const hasAppId = myappid && myappid !== "undefined" && myappid !== "null";

                //** goto login
                const url = new URL(window.location.href);               // https://yourpath/?appid=my_appid
                let start = url.pathname.replace("/app", "");
                let loginPath = start.endsWith('/')             // loginPath = linelogin.html          
                    ? start + "linelogin.html"
                    : start + "/linelogin.html";
                if (hasAppId) {
                    redirectLogin = (url.origin + loginPath + url.search); // https://yourpath/linelogin.html?appid=my_appid
                }
                else {
                    redirectLogin = (url.origin + loginPath);              // https://yourpath/linelogin.html
                }
            }
            this.logout();
            console.log("Redirect to Login Page:", redirectLogin);
            setTimeout(() => {
                window.location.replace(redirectLogin);
            }, 100);
        },
        logout: async function () {
            //** ลบข้อมูลการ login*/
            sessionStorage.removeItem('LIFF_UNIX');
            sessionStorage.removeItem('LIFF_PAYLOAD');
            sessionStorage.removeItem('CRM_REDIRECTLOGIN');
            sessionStorage.removeItem('CRM_APPID');
            sessionStorage.removeItem('CRM_SUBDOMAIN');

        },







    };


})(window);