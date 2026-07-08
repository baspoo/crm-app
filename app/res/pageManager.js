/**
 * Page Manager (Router)
 * หน้าที่: Fetch HTML, จัดการ Cache, รัน Script ใน HTML Fragment, เปิด Iframe และจัดการ Console UI
 */
window.PageManager = (function () {
    'use strict';

    const htmlCache = {};
    let contentContainer = null;
    let configRoutes = {}; // เก็บ Path จาก config.json

    // --- ตัวแปรสำหรับจัดการ Iframe Callback ---
    let currentIframeOnClose = null;
    let currentIframeResponse = null;

    // --- ตัวแปรสำหรับจัดการ Modal Callback (ปรับเป็น Stack สำหรับซ้อนเลเยอร์) ---
    let modalStack = [];


    // ฟังก์ชันสำคัญ: แปะ HTML และบังคับให้เบราว์เซอร์รันแท็ก <script>
    function setInnerHTMLAndExecuteScripts(element, htmlContent, useAnimation = true) {
        element.innerHTML = htmlContent;
        if (useAnimation) {
            // ลบคลาส fade-in เดิมออกก่อน แล้วค่อยใส่ใหม่เพื่อให้เกิด Animation ทวนซ้ำ
            element.classList.remove('fade-in');
            void element.offsetWidth; // Trigger Reflow
            element.classList.add('fade-in');
        }

        const scripts = element.querySelectorAll('script');
        scripts.forEach(oldScript => {
            const newScript = document.createElement('script');
            Array.from(oldScript.attributes).forEach(attr => newScript.setAttribute(attr.name, attr.value));
            newScript.appendChild(document.createTextNode(oldScript.innerHTML));
            oldScript.parentNode.replaceChild(newScript, oldScript);
        });
    }

    // ==========================================
    // Console Manager สำหรับจัดการ Header/Footer
    // ==========================================
    const ConsoleManager = {
        _getHeader: () => document.getElementById('console-header'),
        _getFooter: () => document.getElementById('console-footer'),
        _getTitle: () => document.getElementById('console-page-title'),

        updateConsole: function () {
            if (window.updateConsoleHeader && typeof window.updateConsoleHeader === 'function')
                window.updateConsoleHeader();
        },
        navDirectConsole: function (pageKey, payload = null) {
            if (window.navigateConsole && typeof window.navigateConsole === 'function')
                window.navigateConsole(pageKey, payload);
        },
        visible: function (show) {
            this.activeTop(show);
            this.activeBot(show);
        },

        activeTop: function (show, showPoint = true) {
            const el = this._getHeader();
            if (el) el.style.display = show ? '' : 'none';
            document.getElementById('header-right').style.display = showPoint ? '' : 'none';
            this.updateConsole();
        },

        activeBot: function (show) {
            const el = this._getFooter();
            if (el) el.style.display = show ? '' : 'none';
            this.updateConsole();
        },

        setTitle: function (titleText) {
            const el = this._getTitle();
            if (el) el.textContent = titleText;
            this.updateConsole();
        }
    };

    return {
        console: ConsoleManager,

        setupRoutes: async function (config, preloadedLayoutHtml, preloadedSystemHtml) {
            configRoutes = { ...config.mainPage, ...config.subPage, ...config.systemPage };

            // ทำการ assign DOM ที่โหลด text มารอไว้แล้วทีเดียว
            //[ CONSOLE-LAYOUT ]
            if (preloadedLayoutHtml) {
                const root = document.getElementById('app-layout-root') || document.body;
                setInnerHTMLAndExecuteScripts(root, preloadedLayoutHtml, false);
                contentContainer = document.getElementById('main-content');
                this.console.visible(false);
            }
            //[ SYSTEM-LAYOUT ]
            if (preloadedSystemHtml) {
                let overlayRoot = document.getElementById('system-overlay-root');
                if (!overlayRoot) {
                    overlayRoot = document.createElement('div');
                    overlayRoot.id = 'system-overlay-root';
                    overlayRoot.style.position = 'absolute';
                    overlayRoot.style.inset = '0';
                    overlayRoot.style.zIndex = '50';
                    overlayRoot.style.pointerEvents = 'none';
                    const appRoot = document.querySelector('.app-root-container');
                    if (appRoot) appRoot.appendChild(overlayRoot);
                    else document.body.appendChild(overlayRoot);
                }
                setInnerHTMLAndExecuteScripts(overlayRoot, preloadedSystemHtml, false);
            }
            console.log("[PageManager] ✅ Setup Page assigned DOM elements.");

            //[ PRELOAD-MAIN-PAGES ]
            if (config.mainPage) {
                let fetchPromises = [];
                for (const key in config.mainPage) {
                    fetchPromises.push(
                        fetchPage(`${config.mainPage[key].path}`, APP_VERSION)
                            .then(html => htmlCache[key] = html)
                            .catch(err => console.error(`Failed to preload ${key}:`, err))
                    );
                }
                Promise.all(fetchPromises);
            }
            console.log("[PageManager] ✅ Preloaded layout, system, and main pages.");
        },



        setContentContainer: function (element) {
            contentContainer = element;
            console.log(`contentContainer = ${element}`);
        },

        updatePoints: async function (forceUpdate = false) {
            if (forceUpdate) await CrmApi.getProfile();
            ConsoleManager.updateConsole();
            if (window.updatePoints && typeof window.updatePoints === 'function') {
                // เอาไปใส่ในหน้า home / profile 
                window.updatePoints();
            }
        },

        // --- ระบบ Loading กลางของ Project ---
        showLoading: function (text = "Loading...") {
            const overlay = document.getElementById('global-loading-overlay');
            const textEl = document.getElementById('global-loading-text');
            if (overlay) {
                if (textEl) textEl.textContent = text;
                overlay.classList.remove('hidden');
                void overlay.offsetWidth;
                overlay.classList.remove('hidden-fade'); // เอาคลาสซ่อนออกแบบมี fade ออก
            }
        },

        hideLoading: function () {
            const overlay = document.getElementById('global-loading-overlay');
            if (overlay) {
                overlay.classList.add('hidden-fade'); // สั่งให้เริ่ม fade out
                setTimeout(() => {
                    overlay.classList.add('hidden'); // ซ่อนจริงๆ หลัง fade จบ
                }, 300);
            }
        },

        // โหลดหน้าย่อย
        loadPage: async function (pageKey, payload = null) {
            if (!contentContainer) return console.error("Content container not set!");

            const route = configRoutes[pageKey];
            if (!route) return console.error("Route not found:", pageKey);

            const defaultTitle = pageKey.charAt(0).toUpperCase() + pageKey.slice(1);
            this.console.setTitle(route.title ? route.title : defaultTitle);

            this.console.activeTop(route.top, route.showPoint != undefined ? route.showPoint : true);

            window.onMainPageInit = null;
            setTimeout(() => {
                if (typeof window.onMainPageInit === 'function') {
                    window.onMainPageInit(payload);
                }
            }, 50);


            if (htmlCache[pageKey] && appConfig.setting.cachePage) {
                setInnerHTMLAndExecuteScripts(contentContainer, htmlCache[pageKey]);
                return;
            }

            this.showLoading();
            contentContainer.innerHTML = '<div style="padding: 2.5rem; text-align: center; color: #9ca3af;"></div>';

            try {
                const v = appConfig.setting.cachePage ? APP_VERSION : Date.now();
                const html = await fetchPage(`${route.path}`, v);
                this.hideLoading();
                if (html) {
                    htmlCache[pageKey] = html;
                    setInnerHTMLAndExecuteScripts(contentContainer, html);
                }
            } catch (e) {
                contentContainer.innerHTML = '<div style="padding: 2.5rem; text-align: center; color: #ef4444;">Error loading page</div>';
            }
        },


        // --- ระบบ Modal (แบบ Stack ซ้อนทับกัน A ล่าง B บน) ---
        openModal: async function (pageKey, payload = null, onclose = null, showCloseBtn = true) {
            const route = configRoutes[pageKey];
            if (!route) return console.error("Route not found for modal:", pageKey);

            const x = route.full ? 100 : route.x !== undefined ? Number(route.x) : 90;
            const y = route.full ? 100 : route.y !== undefined ? Number(route.y) : 90;

            // ชี้ไปที่ Container ที่เตรียมไว้แล้วใน console.html
            const container = document.getElementById('sys-modal-container');
            const backdrop = document.getElementById('sys-modal-backdrop');
            const closeBtnEl = document.getElementById('btn-modal-close-float');
            const contentWrap = document.getElementById('sys-modal-content-wrap');

            if (!container || !contentWrap) {
                return console.error("Modal containers not found! Check console.html");
            }

            if (closeBtnEl) closeBtnEl.style.display = showCloseBtn ? 'flex' : 'none';

            // 1. จัดการขนาด Layout ให้ตรงกับ Modal ชั้นบนสุด
            if (x === 100 && y === 100) {
                container.classList.add('fullscreen');
                if (backdrop) backdrop.style.display = 'none';
            } else {
                container.classList.remove('fullscreen');
                container.style.width = `${x}%`;
                container.style.height = `${y}%`;
                container.style.top = `${(100 - y) / 2}%`;
                container.style.left = `${(100 - x) / 2}%`;
                if (backdrop) backdrop.style.display = 'block';
            }

            // แสดงกล่องขึ้นมา
            container.classList.add('show');

            // 2. ซ่อน Modal ชั้นล่าง (A) ถ้ามี เพื่อพัก State ไว้
            if (modalStack.length > 0) {
                const currentTop = modalStack[modalStack.length - 1];
                currentTop.scrollTop = contentWrap.scrollTop; // จำตำแหน่ง Scroll
                if (currentTop.element) currentTop.element.style.display = 'none'; // ซ่อน
            }

            // 3. สร้าง Layer ใหม่สำหรับ Modal ตัวใหม่ (B)
            const newModalPage = document.createElement('div');
            newModalPage.className = 'modal-page-instance';
            newModalPage.style.width = '100%';
            newModalPage.style.minHeight = '100%';
            newModalPage.style.display = 'flex';
            newModalPage.style.flexDirection = 'column';
            newModalPage.style.position = 'relative';
            newModalPage.style.backgroundColor = 'inherit';

            // ใส่ตัวโหลดชั่วคราว
            newModalPage.innerHTML = '<div style="display:flex; justify-content:center; align-items:center; height:100%; min-height: 200px; color:#9ca3af;">กำลังโหลด...</div>';
            contentWrap.appendChild(newModalPage);
            contentWrap.scrollTop = 0; // รีเซ็ต Scroll ให้ชั้นใหม่

            // 4. บันทึก State ของชั้นนี้ลง Stack
            const modalState = {
                pageKey: pageKey,
                element: newModalPage,
                payload: payload,
                response: { demo: true },
                onClose: onclose,
                scrollTop: 0
            };
            modalStack.push(modalState);

            // เซ็ตตัวแปร Global ให้ชี้มาที่ Modal ชั้นบนสุด
            window.currentModalPayload = modalState.payload;
            window.currentModalResponse = modalState.response;

            try {
                let html = appConfig.setting.cachePage ? htmlCache[pageKey] : null;
                if (!html) {
                    const v = appConfig.setting.cachePage ? APP_VERSION : Date.now();
                    html = await fetchPage(`${route.path}`, v);
                    if (html) htmlCache[pageKey] = html;
                    else throw new Error(`Failed to load ${route.path}`);
                }

                let processedHtml = html;

                // ถอดเอาเฉพาะเนื้อหาใน <body> เพื่อรักษาสไตล์และโครงสร้าง
                const bodyRegex = /<body([^>]*)>([\s\S]*?)<\/body>/i;
                const bodyMatch = html.match(bodyRegex);
                if (bodyMatch) {
                    const bodyContent = bodyMatch[2];
                    processedHtml = bodyContent;
                }

                // สับสวิตช์ฆ่า Tailwind CDN ออกจากไฟล์ลูก
                processedHtml = processedHtml.replace(/<script[^>]*tailwindcss\.com[^>]*><\/script>/gi, '');

                // แปะลงใน Layer ใหม่
                setInnerHTMLAndExecuteScripts(newModalPage, processedHtml, false);

                // ส่ง Callback ให้ทำงานเมื่อโหลดเสร็จ
                setTimeout(() => {
                    if (typeof window.onInit === 'function') {
                        console.log("onInit call for", pageKey);
                        console.log("onInit call payload", modalState.payload);
                        console.log("onInit call response", modalState.response);
                        window.onInit(modalState.payload, modalState.response);
                    }
                }, 50);

            } catch (e) {
                console.error("Modal Error:", e);
                newModalPage.innerHTML = '<div style="padding: 2.5rem; text-align: center; color: #ef4444;">เกิดข้อผิดพลาดในการโหลดหน้า</div>';
            }
        },

        closeModal: function () {
            if (modalStack.length === 0) return;

            const container = document.getElementById('sys-modal-container');
            const backdrop = document.getElementById('sys-modal-backdrop');
            const contentWrap = document.getElementById('sys-modal-content-wrap');

            // 1. ดึง Modal ตัวบนสุด (B) ออกจาก Stack
            const topModal = modalStack.pop();

            // ลบ DOM Layer ตัวบนสุดทิ้ง
            if (topModal.element) topModal.element.remove();

            // เรียก Callback ของตัวบนสุด
            if (typeof topModal.onClose === 'function') {
                try {
                    topModal.onClose(topModal.response);
                } catch (e) {
                    console.error("Error executing modal onclose callback:", e);
                }
            }

            // เคลียร์ Global ก่อน
            window.currentModalPayload = null;
            window.currentModalResponse = null;
            window.onInit = null;

            // 2. ตรวจสอบว่ายังมีชั้นล่าง (A) รออยู่หรือไม่
            if (modalStack.length > 0) {
                const previousModal = modalStack[modalStack.length - 1];

                // แสดงผลชั้นล่าง (A) กลับมา
                if (previousModal.element) previousModal.element.style.display = 'flex';

                // คืนตำแหน่ง Scroll ให้ชั้นล่าง
                if (contentWrap) contentWrap.scrollTop = previousModal.scrollTop;

                // คืนค่า Global ให้กลับเป็นของชั้นล่าง (A)
                window.currentModalPayload = previousModal.payload;
                window.currentModalResponse = previousModal.response;

                // ปรับขนาด Container ให้ตรงกับ Config ของชั้นล่าง
                const route = configRoutes[previousModal.pageKey];
                if (route) {
                    const x = route.full ? 100 : route.x !== undefined ? Number(route.x) : 90;
                    const y = route.full ? 100 : route.y !== undefined ? Number(route.y) : 90;
                    if (x === 100 && y === 100) {
                        container.classList.add('fullscreen');
                        if (backdrop) backdrop.style.display = 'none';
                    } else {
                        container.classList.remove('fullscreen');
                        container.style.width = `${x}%`;
                        container.style.height = `${y}%`;
                        container.style.top = `${(100 - y) / 2}%`;
                        container.style.left = `${(100 - x) / 2}%`;
                        if (backdrop) backdrop.style.display = 'block';
                    }
                }
            } else {
                // ไม่มีเหลือแล้ว (ปิด A) ปิดกล่องใหญ่ทิ้งกลับหน้า Home
                if (container) container.classList.remove('show');
                if (backdrop) backdrop.style.display = 'none';
            }
        },

        // โหลด Iframe
        openIframe: function (url, x = 0, y = 0, payload = null, onclose = null, showCloseBtn = true) {
            const container = document.getElementById('iframe-container');
            const iframe = document.getElementById('main-iframe');

            const separator = url.includes('?') ? '&' : '?';
            url = url + separator + 'v=' + APP_VERSION;

            if (container && iframe) {
                // เซฟใส่ตัวแปรกลางแทนการผูกกับ `this` 
                currentIframeOnClose = onclose;
                currentIframeResponse = {};

                // 1. จัดการขนาดและตำแหน่ง
                if (x === 0 && y === 0) {
                    // Fullscreen
                    container.classList.add('fullscreen');
                } else {
                    // Custom Size (จัดให้อยู่กึ่งกลาง)
                    container.classList.remove('fullscreen');
                    container.style.width = `${x}%`;
                    container.style.height = `${y}%`;
                    container.style.top = `${(100 - y) / 2}%`;
                    container.style.left = `${(100 - x) / 2}%`;
                }
                // ควบคุมการแสดงปุ่มปิด

                const closeBtnEl = document.getElementById('btn-iframe-close-float');
                if (closeBtnEl) {
                    closeBtnEl.style.display = showCloseBtn ? 'flex' : 'none';
                }

                // 2. ผูก Event ตอนโหลด Iframe เสร็จเพื่อเรียก oninit และจัดการ Scrollbar
                iframe.onload = () => {
                    try {
                        // --- Inject Global Scrollbar Style ---
                        // เมื่อเป็นโดเมนเดียวกัน จะสามารถทะลุเข้าไปสั่ง CSS ลับได้
                        const doc = iframe.contentDocument || iframe.contentWindow.document;
                        if (doc) {
                            const style = doc.createElement('style');
                            style.innerHTML = `
                                html { overflow: hidden; height: 100%; }
                                body { overflow-y: auto; height: 100%; }
                                ::-webkit-scrollbar { width: 8px !important; height: 8px !important; }
                                ::-webkit-scrollbar-track { background: transparent !important; margin: 16px 0 !important; }
                                ::-webkit-scrollbar-thumb { background-color: rgba(148, 163, 184, 0) !important; border-radius: 10px !important; border: 2px solid transparent !important; background-clip: padding-box !important; transition: background-color 0.3s; }
                                body:hover::-webkit-scrollbar-thumb, *:hover::-webkit-scrollbar-thumb { background-color: rgba(148, 163, 184, 0.4) !important; }
                                *:hover::-webkit-scrollbar-thumb:hover { background-color: rgba(148, 163, 184, 0.7) !important; }
                            `;
                            doc.head.appendChild(style);
                        }
                    } catch (e) {
                        console.warn("Iframe style injection blocked due to Cross-Origin restrictions.", e);
                    }

                    try {
                        if (iframe.contentWindow && typeof iframe.contentWindow.onInit === "function") {
                            iframe.contentWindow.onInit(payload, currentIframeResponse);
                        }
                    } catch (e) {
                        console.warn("Iframe onInit access blocked or missing.", e);
                    }
                };

                // 3. โหลด URL และแสดงผล
                iframe.src = url;
                container.classList.add('show');

                // 4. จัดการฉากหลังดำ (Backdrop) กรณีไม่เต็มจอ
                let backdrop = document.getElementById('iframe-backdrop');
                if (x !== 0 || y !== 0) {
                    if (!backdrop) {
                        backdrop = document.createElement('div');
                        backdrop.id = 'iframe-backdrop';
                        backdrop.style.position = 'absolute';
                        backdrop.style.inset = '0';
                        backdrop.style.backgroundColor = 'rgba(0,0,0,0.6)';
                        backdrop.style.zIndex = '39'; // ให้อยู่ใต้ container (z-40)

                        // กดฉากหลังแล้วปิด
                        backdrop.onclick = () => { this.closeIframe(); };

                        container.parentNode.insertBefore(backdrop, container);
                    }
                    backdrop.style.display = 'block';
                } else {
                    if (backdrop) backdrop.style.display = 'none';
                }

            } else if (contentContainer) {
                // Fallback กรณีไม่มีโครงสร้าง Iframe แบบใหม่
                contentContainer.innerHTML = `<iframe src="${url}" style="width:100%; height:100%; border:none;" class="fade-in"></iframe>`;
            }
        },

        closeIframe: function () {
            console.log("closeIframe 1");
            const container = document.getElementById('iframe-container');
            const iframe = document.getElementById('main-iframe');
            const backdrop = document.getElementById('iframe-backdrop');

            if (container && iframe) {
                console.log(JSON.stringify(currentIframeResponse));
                container.classList.remove('show');
                iframe.src = '';

                // บังคับ "ลบ" Backdrop ออกจาก DOM ไปเลยเพื่อป้องกันปัญหาค้าง
                if (backdrop) {
                    backdrop.remove();
                }

                // ตรวจสอบและเรียกใช้งาน Callback ตอนปิด (อ่านจาก Scope นอกแทน this)
                if (typeof currentIframeOnClose === 'function') {
                    try {
                        currentIframeOnClose(currentIframeResponse);
                    } catch (e) {
                        console.error("Error executing iframe onclose callback:", e);
                    }
                }

                // เคลียร์ค่าทิ้ง
                currentIframeOnClose = null;
                currentIframeResponse = null;
            }
        },

        // โหลดหน้า Login Overlay
        loadLoginScreen: async function () {
            try {
                const path = appConfig.systemPage.login.path;
                const res = await fetch(`${path}?v=${APP_VERSION}`);
                if (res.ok) {
                    const html = await res.text();
                    let loginRoot = document.getElementById('login-overlay-root');
                    if (!loginRoot) {
                        loginRoot = document.createElement('div');
                        loginRoot.id = 'login-overlay-root';
                        loginRoot.style.position = 'absolute';
                        loginRoot.style.inset = '0';
                        loginRoot.style.zIndex = '30';
                        loginRoot.style.pointerEvents = 'none';
                        document.querySelector('.app-root-container').appendChild(loginRoot);
                    }
                    setInnerHTMLAndExecuteScripts(loginRoot, html, false);
                    console.log("[PageManager] ✅ Login screen loaded.");
                }
            } catch (err) {
                console.error("[PageManager] Failed to load login screen:", err);
            }
        },

        // --- ระบบ Popup / Dialog ---
        showPopup: function ({ type, header, message, image, onYes, onNo, onOk }) {
            const overlay = document.getElementById('sys-popup-overlay');
            const card = document.getElementById('sys-popup-card');
            if (!overlay || !card) return console.error("System page not loaded yet!");

            // ควบคุมเนื้อหา
            document.getElementById('sys-popup-title').textContent = header || "";
            document.getElementById('sys-popup-message').textContent = message || "";

            const emojiEl = document.getElementById('sys-popup-emoji');
            const imgEl = document.getElementById('sys-popup-img');
            const iconContainer = document.getElementById('sys-popup-icon-container');

            if (image) {
                const isUrl = image.includes('/') || image.includes('http') || image.includes('.');
                if (isUrl) {
                    imgEl.src = image;
                    imgEl.classList.remove('hidden');
                    emojiEl.classList.add('hidden');
                } else {
                    emojiEl.textContent = image;
                    emojiEl.classList.remove('hidden');
                    imgEl.classList.add('hidden');
                }
            } else {
                let defaultEmoji = "ℹ️";
                if (type === 'complete') defaultEmoji = "✅";
                else if (type === 'failed') defaultEmoji = "❌";
                else if (type === 'confirm') defaultEmoji = "❓";

                emojiEl.textContent = defaultEmoji;
                emojiEl.classList.remove('hidden');
                imgEl.classList.add('hidden');
            }

            const btnOk = document.getElementById('sys-popup-btn-ok');
            const btnYes = document.getElementById('sys-popup-btn-yes');
            const btnNo = document.getElementById('sys-popup-btn-no');

            // ใช้ Inline CSS ควบคุมสี Theme แทน Tailwind classes
            if (type === 'complete') {
                iconContainer.style.backgroundColor = '#ecfdf5';
                iconContainer.style.color = '#10b981';
                btnOk.style.backgroundColor = '#059669';
            } else if (type === 'failed') {
                iconContainer.style.backgroundColor = '#fef2f2';
                iconContainer.style.color = '#ef4444';
                btnOk.style.backgroundColor = '#dc2626';
            } else {
                // Default (Info / Confirm)
                iconContainer.style.backgroundColor = '#eff6ff';
                iconContainer.style.color = '#3b82f6';
                btnOk.style.backgroundColor = ''; // ใช้สีจาก CSS class หลัก
                btnYes.style.backgroundColor = '';
            }

            // จัดการปุ่มกดตามประเภท
            const actionsConfirm = document.getElementById('sys-popup-actions-confirm');
            const actionsOk = document.getElementById('sys-popup-actions-ok');

            if (type === 'confirm') {
                actionsConfirm.classList.remove('hidden');
                actionsOk.classList.add('hidden');

                btnYes.onclick = () => { PageManager.hidePopup(); if (onYes) onYes(); };
                btnNo.onclick = () => { PageManager.hidePopup(); if (onNo) onNo(); };
            } else {
                actionsConfirm.classList.add('hidden');
                actionsOk.classList.remove('hidden');

                btnOk.onclick = () => { PageManager.hidePopup(); if (onOk) onOk(); };
            }

            // แสดง Popup ด้วยคลาส show
            overlay.classList.add('show');
        },

        hidePopup: function () {
            const overlay = document.getElementById('sys-popup-overlay');
            if (overlay) overlay.classList.remove('show');
        },

        showConfirm: function (header, message, image, onYes, onNo) {
            this.showPopup({ type: 'confirm', header, message, image, onYes, onNo });
        },

        showOk: function (header, message, image, onOk) {
            this.showPopup({ type: 'ok', header, message, image, onOk });
        },

        showFailed: function (header, message, image, onOk) {
            this.showPopup({ type: 'failed', header, message, image, onOk });
        },

        showComplete: function (header, message, image, onOk) {
            this.showPopup({ type: 'complete', header, message, image, onOk });
        },

        // --- ระบบ Top Message Pop (Toast) ---
        showToast: function (message, icon) {
            const overlay = document.getElementById('sys-toast-overlay');
            const messageEl = document.getElementById('sys-toast-message');
            const emojiEl = document.getElementById('sys-toast-emoji');
            const imgEl = document.getElementById('sys-toast-img');

            if (!overlay || !messageEl) return;

            messageEl.textContent = message || "";

            if (icon) {
                const isUrl = icon.includes('/') || icon.includes('http') || icon.includes('.');
                if (isUrl) {
                    imgEl.src = icon;
                    imgEl.classList.remove('hidden');
                    emojiEl.classList.add('hidden');
                } else {
                    emojiEl.textContent = icon;
                    emojiEl.classList.remove('hidden');
                    imgEl.classList.add('hidden');
                }
            } else {
                emojiEl.textContent = "🔔";
                emojiEl.classList.remove('hidden');
                imgEl.classList.add('hidden');
            }

            overlay.classList.add('show');

            if (this._toastTimeout) clearTimeout(this._toastTimeout);
            this._toastTimeout = setTimeout(() => {
                this.hideToast();
            }, 3000);
        },

        hideToast: function () {
            const overlay = document.getElementById('sys-toast-overlay');
            if (overlay) overlay.classList.remove('show');
        },

        // --- ระบบ Bottom Direct Link Popup ---
        showDirectLink: function (url) {
            const overlay = document.getElementById('sys-link-overlay');
            const inputEl = document.getElementById('sys-link-input');

            if (!overlay || !inputEl) return;

            inputEl.value = url;

            const btnCopy = document.getElementById('sys-link-btn-copy');
            if (btnCopy) {
                btnCopy.textContent = "Copy";
                btnCopy.style.backgroundColor = "var(--color-primary-light, #eff6ff)";
                btnCopy.style.color = "var(--color-primary, #2563eb)";
            }

            overlay.classList.add('show');
            this._currentDirectLink = url;
        },

        hideDirectLink: function () {
            const overlay = document.getElementById('sys-link-overlay');
            if (overlay) overlay.classList.remove('show');
        },

        copyDirectLink: function () {
            const inputEl = document.getElementById('sys-link-input');
            if (!inputEl) return;

            inputEl.select();
            inputEl.setSelectionRange(0, 99999);

            navigator.clipboard.writeText(inputEl.value).then(() => {
                const btnCopy = document.getElementById('sys-link-btn-copy');
                if (btnCopy) {
                    btnCopy.textContent = "Copied!";
                    btnCopy.style.backgroundColor = "#ecfdf5"; // emerald-50
                    btnCopy.style.color = "#059669"; // emerald-600
                    setTimeout(() => {
                        btnCopy.textContent = "Copy";
                        btnCopy.style.backgroundColor = "var(--color-primary-light, #eff6ff)";
                        btnCopy.style.color = "var(--color-primary, #2563eb)";
                    }, 2000);
                }
            }).catch(err => {
                console.error("Could not copy link text:", err);
            });
        },

        openDirectLink: function () {
            if (this._currentDirectLink) {
                window.open(this._currentDirectLink, '_blank');
                this.hideDirectLink();
            }
        }
    };
})();