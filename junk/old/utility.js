

// get version
var version = "1.0.0 (demo)";
var buildId = "0 (demo)";
var appVersion = "1.0 (demo)";
var caching = false;
var backgroundColor = "gray";
var backgroundImage = "none";
var canvas_x = 420;
var canvas_y = 720;
var borderRadius = 10;
var loadingDefault = "TemplateData/loading.gif?v=1.0";
var loadingbackgroundDefault = "TemplateData/bg.png?v=1.0";
const versionElement = document.getElementById("version");
const params = new URLSearchParams(location.search);
window.appData ??= {};

const currentScript = document.currentScript;
const url = new URL(currentScript.src);
window.htmlCacheVersion = url.searchParams.get("v") || "0";
let initializeData = {};

//** LINE OPEN URL OTHER */
let _liffReady = false;
let _liffLoading = null;
function loadLiff() {
  if (_liffReady && window.liff) return Promise.resolve(window.liff);
  if (_liffLoading) return _liffLoading;
  _liffLoading = new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.src = "https://static.line-scdn.net/liff/edge/2/sdk.js";
    script.onload = async () => {
      try {
        const liffPayload = sessionStorage.getItem('LIFF_PAYLOAD');
        const liffId = liffPayload ? JSON.parse(liffPayload).liffId : "";
        await liff.init({ liffId: liffId });
        _liffReady = true;
        resolve(window.liff);
      } catch (err) {
        reject(err);
      }
    };
    script.onerror = reject;
    document.head.appendChild(script);
  });
  return _liffLoading;
}




async function awake() {


  // Check App Id..?
  const appid = checkAppId();

  // Tittle Name...
  const rawPayload = sessionStorage.getItem("LIFF_PAYLOAD");
  if (rawPayload) {
    try {
      const payload = JSON.parse(rawPayload);
      loginToApp(appid, payload);
      if (payload.storeInfo && payload.storeInfo.storeName) {

        //Tittle
        document.title = payload.storeInfo.storeName;

        //Style Loading Screen
        initializeData = payload.initializeData;
        if (initializeData)
          setupLoadingScreen(initializeData);

        loadLiff();

      }
    }
    catch (e) {
      console.error("Error parsing payload from sessionStorage:", e);
    }
  }

  return;
}


let loginStatus = "none";
let loginResponseData = null;
let themeVersion = "";
async function loginToApp(appid, payload) {

  fetch(`StreamingAssets/AssetsBundle/version/${payload.initializeData.themeId}.txt?v=${new Date().getTime()}`)
    .then(r => r.text())
    .then(t => {
      themeVersion = t;
    });

  loginStatus = "logged-in";
  const API_URL = location.origin.includes("gamestore-gg.1mobystudio.com")
    ? 'https://aws-bug-parse-gamification.thelastbug.co/open/crm_publicAuth'
    : 'https://reflex-crm-mdgl7ag2.g.1mobystudio.com/open/crm_publicAuth';


  let body = {
    gameId: appid,
    uid: payload.userId,
    idToken: payload.idToken
  };
  const resp = await fetch(API_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  });

  const result = await resp.json();
  console.log(result);
  if (!result?.ok) {
    loginStatus = "failed";
    if (result.code === "USER_NOT_FOUND") {
      //** NEW USER */
      // ToDo : goto login in-App (full-loop)...
    }
    else if (result.code === "INVALID_TOKEN") {
      //** INVALID TOKEN */
      // ToDo : goto re-login LINE...
      redirectTologin();
    }
  }
  else {
    //** OK */
    // ToDo : goto App Faster...
    if (result.response && result.response.data && result.response.code == 200) {
      loginStatus = "completed";
      loginResponseData = result.response.data;
    }
    else {
      loginStatus = "failed";
    }
  }

}



function getDomainOrSubdomain() {
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
}
function checkAppId() {


  //** 1.มี accessToken เส้นใหญ่ไฟกระพริบ เข้าไปเลย ไม่ต้องเช็คอะไรแล้ว (สำหรับ DEV เทสเอง) */
  const accessToken = params.get('accessToken');
  if (accessToken) {
    return params.get('appid'); // ถ้ามาอย่างถูกต้อง มันจะมี appid มาเอง *ถ้าไม่มีก็ไม่ต้องสนใจ..  ปล่อยมันพัง มาแปลก?
  }


  //** 2.ดึง subdomain และ appid จาก url หน้านี้*/
  const mysubDomain = getDomainOrSubdomain();
  const myappid = params.get('appid');
  if (!myappid && !mysubDomain) {
    //** ไม่มีอะไรสักอย่าง ถีบออกไปหน้า landing page.... */
    window.location.href = "https://reflexstudio.co/";
    return null;
  }

  //** 3.ดึง subdomain และ appid จากหน้า linelogin */
  let appOk = true;
  const appId = sessionStorage.getItem('CRM_APPID');
  const subDomain = sessionStorage.getItem('CRM_SUBDOMAIN');

  //** 4.ไม่มี  appId จบตรงนี้เลย.... กลับไป login ใหม่*/
  if (!appId) {
    appOk = false;
  }
  //** 5.มี subDomain แต่ไม่ตรงกัน มาไง? ไม่ผ่าน..ถีบไป login */
  if (mysubDomain && subDomain && mysubDomain !== subDomain) {
    appOk = false;
  }
  //** 6.มี appid มาครบ ทั้งหน้านี้และได้จาก login แต่ดันไม่ตรงกัน.. กลับไป login ใหม่ */
  if (myappid && appId && myappid !== appId) {
    appOk = false;
  }
  //** 7.ไม่มี payload สำหรับ store แปลกๆ... กลับไป login ใหม่ */
  if (!sessionStorage.getItem('LIFF_PAYLOAD')) {
    appOk = false;
  }

  //** 8.มีครบทุกอย่าง แต่หมดเวลา.... กลับไป login ใหม่ */
  let now = Date.now();
  let lastLogin = parseInt(sessionStorage.getItem('LIFF_UNIX') || 0);
  let timeout = (lastLogin + 60000 * 30) < now; // 30 นาที.
  if (timeout)
    appOk = false;

  if (appOk) {
    return appId;
  }
  else {
    redirectTologin();
    return null;
  }
}

function redirectTologin() {
  let redirectLogin = sessionStorage.getItem('CRM_REDIRECTLOGIN');
  if (!redirectLogin || redirectLogin == "undefined" || redirectLogin == "null" || redirectLogin == "") {
    console.error("CRM_REDIRECTLOGIN IS NULL");


    //** ดูจาก URL หน้าเว็ปเท่านั้นมี appid ไหม ถ้ามีกลับไปแบบมี ถ้าไม่มี กลับไปตรงๆได้เลย */
    const myappid = params.get('appid');
    const hasAppId = myappid && myappid !== "undefined" && myappid !== "null";

    //** goto login
    const url = new URL(window.location.href);               // https://yourpath/?appid=my_appid
    const loginPath = url.pathname.endsWith('/')             // loginPath = linelogin.html          
      ? url.pathname + "linelogin.html"
      : url.pathname + "/linelogin.html";
    if (hasAppId) {
      redirectLogin = (url.origin + loginPath + url.search); // https://yourpath/linelogin.html?appid=my_appid
    }
    else {
      redirectLogin = (url.origin + loginPath);              // https://yourpath/linelogin.html
    }
  }
  sessionStorage.removeItem('LIFF_UNIX');
  sessionStorage.removeItem('LIFF_PAYLOAD');
  sessionStorage.removeItem('CRM_REDIRECTLOGIN');
  sessionStorage.removeItem('CRM_APPID');
  sessionStorage.removeItem('CRM_SUBDOMAIN');
  console.log("Redirect to Login Page:", redirectLogin);
  setTimeout(() => {
    window.location.replace(redirectLogin);
  }, 100);
}

function setupLoadingScreen(initData) {
  const origin = window.location.origin;

  /* ---------- Background Color ---------- */
  if (initData && initData.loadingBgColor) {
    document.body.style.backgroundColor = initData.loadingBgColor;
  }

  /* ---------- Background Image ---------- */
  if (initData && initData.customAssets?.loadingBg) {
    const bgUrl = new URL(initData.customAssets.loadingBg, origin).href;
    document.body.style.backgroundImage = `url('${bgUrl}')`;
  }
  else {
    document.body.style.backgroundImage = `url('${loadingbackgroundDefault}')`;
  }
  document.body.style.backgroundRepeat = "no-repeat";
  document.body.style.backgroundPosition = "center";
  document.body.style.backgroundSize = "cover";

  /* ---------- Logo ---------- */
  const mobileSize = 0.8;
  const pcSize = 1.0;
  if (initData && initData.customAssets?.loadingImg) {
    const logoUrl = new URL(initData.customAssets.loadingImg, origin).href;
    console.log(`LOADINAGE CUS : ${logoUrl} ${initData.loadingImg_width} x ${initData.loadingImg_height} `);
    logo.style.width = (initData.loadingImg_width * (isPortraitScreen ? mobileSize : pcSize)) + "px";
    logo.style.height = (initData.loadingImg_height * (isPortraitScreen ? mobileSize : pcSize)) + "px";
    logo.style.backgroundImage = `url('${logoUrl}')`;
  }
  else {
    console.log(`LOADINAGE ORIGINAL`);
    logo.style.backgroundImage = `url('${loadingDefault}')`;
    logo.style.width = (220 * (isPortraitScreen ? mobileSize : pcSize)) + "px";
    logo.style.height = (220 * (isPortraitScreen ? mobileSize : pcSize)) + "px";
  }

  /* ---------- HTML Bg Color ---------- */
  if (initData) {
    if (isPortraitScreen) {
      if (initData.bgColorHtmlMobile)
        backgroundColor = initData.bgColorHtmlMobile;
    }
    else {
      if (initData.bgColorHtmlDesktop)
        backgroundColor = initData.bgColorHtmlDesktop;
    }
  }


  /* ---------- Desktop ---------- */
  if (!isPortraitScreen && initData.desktop) {
    const desktop = initData.desktop;
    backgroundColor = desktop.backgroundColor || backgroundColor;
    backgroundImage = desktop.backgroundImage || backgroundImage;
    if (desktop.x) {
      canvas_x = desktop.x;
      canvas.style.width = canvas_x + "px";
    }
    if (desktop.y) {
      canvas_y = desktop.y;
      canvas.style.height = canvas_y + "px";
    }
    if (desktop.borderRadius) {
      canvas_y = desktop.borderRadius;
      canvas.style.borderRadius = borderRadius + "px";
    }
  }
}

// Function to load version
async function loadVersion() {
  await awake();
  return new Promise((resolve, reject) => {
    var client = new XMLHttpRequest();
    //client.open('GET', 'StreamingAssets/version.txt?v=' + new Date().getTime());
    client.open('GET', 'StreamingAssets/version.txt?v=' + htmlCacheVersion);

    client.onreadystatechange = function () {
      if (client.readyState === 4) { // Ensure the request is complete
        if (client.status === 200) { // Ensure the request was successful
          var json = client.responseText;

          try {
            var obj = JSON.parse(json);
            version = obj.version;
            buildId = obj.build;
            appVersion = obj.appversion;
            caching = obj.caching;
            console.log("Get TextFile Json:", json);
            console.log("version:", version);
            console.log("build:", buildId);
            console.log("appversion:", appVersion);
            console.log("caching:", caching);
            versionElement.textContent = `${version}`;
            console.log("onRepath");
            onRepath();
            preloadUnityFiles();
            console.log("loadVersion done...");
            resolve();
          }
          catch (e) {
            console.error("Error parsing JSON:", e.message);
            console.error("Response received:", json);
            reject("Failed to parse version file");
          }
        } else {
          console.error("Failed to fetch the file. Status:", client.status);
          reject("Failed to fetch version file");
        }
      }
    };
    client.send();
  });
}




async function clearServiceWorker() {
  if ('serviceWorker' in navigator) {
    const registrations = await navigator.serviceWorker.getRegistrations();
    for (let registration of registrations) {
      await registration.unregister();
      window.location.reload();
    }
  }
}
function setupServiceWorker() {
  return new Promise((resolve, reject) => {
    if ("serviceWorker" in navigator) {
      navigator.serviceWorker
        .register(`ServiceWorker.js?v=${buildId}`)
        .then(async function (registration) {
          console.log("ServiceWorker registration successful");

          // Wait until the Service Worker is ready
          await navigator.serviceWorker.ready;
          console.log("ServiceWorker is ready");

          await checkVersion(buildId);
          console.log("checkVersion = done...");

          // Resolve the promise once ready
          resolve();
        })
        .catch(function (error) {
          console.error("ServiceWorker registration failed:", error);
          reject(error); // Reject if registration fails
        });
    } else {
      console.warn("ServiceWorker is not supported");
      resolve(); // Resolve if Service Worker is not supported
    }
  });
}

async function checkVersion(currentVersion) {
  console.log(`checkVersion = start`);
  var current = localStorage.getItem('serviceWorker_version');

  if (current == null) {
    console.log(`checkVersion = firstTime version (${currentVersion})`);
    localStorage.setItem('serviceWorker_version', currentVersion);
    return;
  }
  if (current != currentVersion) {
    localStorage.setItem('serviceWorker_version', currentVersion);
    console.log(`checkVersion = new version (${currentVersion})`);
    await clearCachesServiceWorker();
  }
  else {
    console.log(`checkVersion = continue version (${current} == ${version})`);
  }
}

async function clearCachesServiceWorker() {
  if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
    // Send message to the active Service Worker to clear caches
    console.log("Requesting Service Worker to clear caches...");
    await navigator.serviceWorker.controller.postMessage({ type: "CLEAR_CACHE" });
  } else {
    console.warn("No active Service Worker found. Cannot clear caches.");
  }
}



// Main execution flow
window.addEventListener("load", async function () {
  try {

    await loadVersion();

    if (caching) {
      console.log("SetupServiceWorker...");
      await setupServiceWorker();
    } else {
      console.log("Clearing ServiceWorker...");
      await clearServiceWorker();
    }

    console.log("Loading Unity...");
    loadUnity();

  } catch (error) {
    console.error("Error during initialization:", error);
    versionElement.textContent = error;
  }
});

var container = document.querySelector("#unity-container");
var canvas = document.querySelector("#unity-canvas");
var logo = document.querySelector("#unity-logo");
var warningBanner = document.querySelector("#unity-warning");
canvas.style.visibility = "hidden";

function unityShowBanner(msg, type) { }
var m_done = false;
var m_unityInstance = null;
var mobile = false;
var zipType = ".br";
var buildUrl = "Build/" + version;
var loaderUrl = buildUrl + "/WebGL.loader.js";
var config = {
  dataUrl: buildUrl + "/WebGL.data" + zipType,
  frameworkUrl: buildUrl + "/WebGL.framework.js" + zipType,
  codeUrl: buildUrl + "/WebGL.wasm" + zipType,
  streamingAssetsUrl: "StreamingAssets",
  companyName: "1Moby",
  productName: "GameBundle",
  productVersion: appVersion,
  showBanner: unityShowBanner,
};
function getParams() {
  return `${zipType}?buildId=${buildId}`;
}
function onRepath() {
  var vb = version + " " + buildId;
  console.log(vb);
  buildUrl = "Build/" + version;
  loaderUrl = buildUrl + "/WebGL.loader.js";
  config.dataUrl = buildUrl + "/WebGL.data" + getParams();
  config.frameworkUrl = buildUrl + "/WebGL.framework.js" + getParams();
  config.codeUrl = buildUrl + "/WebGL.wasm" + getParams();
  config.productVersion = vb;
}

function preloadUnityFiles() {
  const files = [
    buildUrl + "/WebGL.data" + getParams(),
    buildUrl + "/WebGL.framework.js" + getParams(),
    buildUrl + "/WebGL.wasm" + getParams()
  ];

  files.forEach(url => {
    const link = document.createElement("link");
    link.rel = "preload";
    link.href = url;
    link.as = "fetch";
    link.crossOrigin = "anonymous";
    document.head.appendChild(link);
  });

  console.log("Unity preload started");
}

var isMobileDevice = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);
var isPortraitScreen = window.innerHeight > window.innerWidth;

if (isMobileDevice || isPortraitScreen) {
  var meta = document.createElement('meta');
  meta.name = 'viewport';
  meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
  document.getElementsByTagName('head')[0].appendChild(meta);

  var inputmobile = document.createElement('meta');
  inputmobile.name = 'mobile-web-app-capable';
  inputmobile.content = 'content=yes';
  document.getElementsByTagName('head')[0].appendChild(inputmobile);

  container.className = "unity-mobile";

  if (isMobileDevice)
    mobile = true;
} else {
  container.className = "unity-desktop";
  canvas.style.width = canvas_x + "px";
  canvas.style.height = canvas_y + "px";
  canvas.style.border = "4px solid rgba(255, 255, 255, 0.2)";
  canvas.style.borderRadius = borderRadius + "px";
  canvas.style.boxShadow = "0 0 30px 10px rgba(0, 0, 0, 0.25)";
  canvas.style.position = "absolute";
  canvas.style.top = "50%";
  canvas.style.left = "50%";
  canvas.style.transform = "translate(-50%, -50%)";
}

function pixelRatio() {
  var screenWidth = window.screen.width;
  var screenHeight = window.screen.height;
  var nativeRatio = window.devicePixelRatio || 1;
  var totalNativePixels = screenWidth * screenHeight * Math.pow(nativeRatio, 2);
  var maxPixelBudget = 2000000;
  var finalRatio = nativeRatio;

  if (totalNativePixels > maxPixelBudget) {
    finalRatio = Math.sqrt(maxPixelBudget / (screenWidth * screenHeight));
    finalRatio = Math.max(1.5, Math.min(finalRatio, 2.0));
  }
  config.devicePixelRatio = finalRatio;
  console.log(`Final Device Pixel Ratio: [${totalNativePixels}]" ---- ${finalRatio}`);
}

logo.style.display = "block";

function loadUnity() {
  pixelRatio();
  console.log("loadUnity... start");
  var script = document.createElement("script");
  script.src = loaderUrl;
  script.onload = () => {
    createUnityInstance(canvas, config, (progress) => {
    }).then((unityInstance) => {
      m_unityInstance = unityInstance;
      m_done = true;

      window.addEventListener('message', function (event) {
        if (event.data && event.data.type === 'formData') {
          callbackIframe(event);
        }
      }, false);

      canvas.addEventListener('focus', function () {
        console.log('Element focused');
        OnFocus(true);
      });
      canvas.addEventListener('blur', function () {
        console.log('Element unfocused');
        OnFocus(false);
      });

      document.addEventListener("visibilitychange", function () {
        if (document.hidden) {
          OnFocus(false);
        } else {
          OnFocus(true);
        }
      });
    }).catch((message) => {
      alert(message);
    });
  };

  document.body.appendChild(script);
}

function onWebGLReady() {
  versionElement.style.display = "none";
  var canvas = document.querySelector("#unity-canvas");
  logo.style.display = "none";
  canvas.style.visibility = "visible";
  document.body.style.backgroundColor = backgroundColor;
  document.body.style.backgroundImage = backgroundImage;
  console.log("Loading Screen Closed by Unity");
}

function OnFocus(focus) {
  if (m_done && mobile) {
    m_unityInstance.SendMessage('HtmlCallback', 'Focus', focus ? "0" : "1");
  }
}

function HtmlMessage(code, str) {

  console.log(`HtmlMessage from Unity: code=${code}, str=${str}`);

  if (code == -1) window.close();
  if (code == 200) onWebGLReady();
  if (code == 0) m_unityInstance.SendMessage('HtmlCallback', 'Callback', (mobile) ? "1" : "0");
  if (code == 1) {
    if (str == 'get') {
      m_unityInstance.SendMessage('HtmlCallback', 'Callback', (document.fullscreenElement) ? "1" : "0");
    }
    else if (str == 'set') {
      if (!document.fullscreenElement) m_unityInstance.SetFullscreen(1);
      else if (document.exitFullscreen) m_unityInstance.SetFullscreen(0);
    }
    else {
      if (str == 'full') m_unityInstance.SetFullscreen(1);
      if (str == 'unfull') m_unityInstance.SetFullscreen(0);
    }
  }
  if (code == 2) window.location.href = str;
  if (code == 3) window.open(str, "_blank");

  if (code == 4) {
    const obj = JSON.parse(str);
    copy(obj.text, obj.x, obj.y);
  }
  if (code == 5) {
    clearCache();
    window.location.reload();
  }
  if (code == 6) window.location.reload();
  if (code == 7) m_unityInstance.SendMessage('HtmlCallback', 'Callback', version + "|" + buildId + "|" + appVersion);
  if (code == 8) {
    console.log(navigator.userAgent);
    m_unityInstance.SendMessage('HtmlCallback', 'Callback', navigator.userAgent);
  }
  if (code == 9) {
    const urlToShare = str;
    const facebookShareUrl = `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(urlToShare)}`;
    window.open(facebookShareUrl, 'Facebook Share', 'width=600,height=400');
  }
  if (code == 10) openExtranalLink(str);


  if (code == 11) {
    const raw = sessionStorage.getItem(str);
    m_unityInstance.SendMessage('HtmlCallback', 'Callback', raw || "");
  }
  if (code == 12) {
    const parts = str.split("|");
    const header = parts[0];
    const message = parts.length > 1 ? parts[1] : "";
    sessionStorage.setItem(header, message);
  }
  if (code == 13) sessionStorage.removeItem(str);
  if (code == 14) {
    const raw = localStorage.getItem(str);
    m_unityInstance.SendMessage('HtmlCallback', 'Callback', raw || "");
  }
  if (code == 15) {
    const parts = str.split("|");
    const header = parts[0];
    const message = parts.length > 1 ? parts[1] : "";
    localStorage.setItem(header, message);
  }
  if (code == 16) localStorage.removeItem(str);
  if (code == 18) {
    if ("vibrate" in navigator) navigator.vibrate(1000);
    else console.log("Vibration not supported");
  }

  if (code == 20) {
    var url = toUtf8(str);
    var image = new Image();
    image.src = url;
    document.write(image.outerHTML);
  }
  if (code == 21) {
    var url = toUtf8(str);
    openPopup(url);
  }
  if (code == 22) {
    const parts = str.split("|");
    const header = parts[0];
    const message = parts.length > 1 ? parts[1] : "";
    openInput(header, message);
  }
  if (code == 23) {
    const obj = JSON.parse(str);
    openIframe(obj.url, obj.x, obj.y, obj.type, obj.payload);
  }
  if (code == 24) {
    const obj = JSON.parse(str);
    updateIframe(obj);
  }
  if (code == 25) openDefaultBrowser(str);
  if (code == 26) {
    const obj = JSON.parse(str);
    callbackExternalApi(obj.functionName, obj.response);
  }
  if (code == 30) checkCameraPermission();






  // ONLY CRM..
  if (code == 1001) redirectTologin();

  if (code == 1002) {
    //** Send loginStatus */
    m_unityInstance.SendMessage('HtmlCallback', 'Callback', loginStatus || "");
  }
  if (code == 1003) {
    //** Send loginResponseData */
    m_unityInstance.SendMessage('HtmlCallback', 'Callback', loginResponseData ? JSON.stringify(loginResponseData) : "");
  }
  if (code == 1004) {
    //** Clear  */
    loginStatus = null;
    loginResponseData = null;
  }
  if (code == 1005) {
    //** Get Theme Bundle Verion  */
    m_unityInstance.SendMessage('HtmlCallback', 'Callback', themeVersion || "");
  }
  if (code == 1006) {
    //** LoginDone  */
    Object.assign(
      window.appData,
      JSON.parse(str)
    );

    console.log("LoginDone...");
    console.log(`appData : ${JSON.stringify(window.appData)}`);
    console.log(`htmlCacheVersion : ${window.htmlCacheVersion}`);

    //appData = JSON.parse(str);
    // urlroot = obj.urlroot;
    // gameId = obj.gameId;
    // statId = obj.statId;
  }



}

function checkCameraPermission() {
  if (!navigator.permissions) {
    console.warn("⚠️ Permissions API not supported in this browser.");
    return;
  }
  navigator.permissions.query({ name: "camera" })
    .then(function (permissionStatus) {
      console.log("📸 Camera permission status:", permissionStatus.state);
      if (permissionStatus.state === "granted") {
        console.log("✅ Camera permission already granted.");
        requestCameraAccess("Camera permission already granted");
      }
      else if (permissionStatus.state === "prompt") {
        console.log("⚠️ Camera permission needs user action.");
        requestCameraAccess("Camera permission needs user action");
      }
      else {
        console.error("❌ Camera permission denied.");
        m_unityInstance.SendMessage('HtmlCallback', 'Callback', "FAILED");
      }
      permissionStatus.onchange = function () {
        console.log("🔄 Camera permission changed to:", permissionStatus.state);
      };
    })
    .catch(function (error) {
      console.error("⚠️ Error checking camera permission:", error);
      m_unityInstance.SendMessage('HtmlCallback', 'Callback', "FAILED");
    });
}

function requestCameraAccess(message) {
  navigator.mediaDevices.getUserMedia({ video: true })
    .then(function (stream) {
      console.log("✅ Camera access granted.");
      m_unityInstance.SendMessage('HtmlCallback', 'Callback', "OK");
      let tracks = stream.getTracks();
      tracks.forEach(track => track.stop());
    })
    .catch(function (error) {
      console.error("❌ Camera access denied:", error.message);
      m_unityInstance.SendMessage('HtmlCallback', 'Callback', "FAILED");
    });
}

function openDefaultBrowser(goto) {
  var ua = navigator.userAgent || navigator.vendor;
  if (/Line|FB_IAB|FBAN|FBAV|Instagram/i.test(ua)) {
    console.log("📌 ตรวจพบว่าเปิดผ่าน In-App Browser");
    var url = goto || window.location.href;
    var userAgent = navigator.userAgent || navigator.vendor;
    if (/android/i.test(userAgent)) {
      var intentUrl = "intent://" + url.replace(/^https?:\/\//, "") + "#Intent;scheme=https;package=com.android.chrome;end;";
      window.location.href = intentUrl;
    }
    else if (/iPad|iPhone|iPod/.test(userAgent) || /Macintosh/.test(userAgent) && "ontouchend" in document) {
      window.open(url, "_blank");
    }
    else {
      console.log("📌 เปิดในเบราว์เซอร์เดิม (ไม่ใช่ In-App Browser)");
    }
  }
  else {
    console.log("(ไม่ใช่ In-App Browser)");
  }
}






//** ExtranalLink */
let currentExternalURL = "";
function openExtranalLink(url) {
  currentExternalURL = url;
  // อัปเดตข้อความใน UI
  const urlTextElement = document.getElementById('ext-url-text');
  if (urlTextElement) {
    urlTextElement.innerText = url;
  }
  // แสดง Overlay และ Popup
  const overlay = document.getElementById('ext-link-overlay');
  const popup = document.getElementById('ext-link-popup');
  overlay.style.display = 'block';
  // ใช้ setTimeout เล็กน้อยเพื่อให้ CSS Transition ทำงาน
  setTimeout(() => {
    overlay.classList.add('active');
    popup.classList.add('active');
  }, 10);
}
function closeExternalLink() {
  const overlay = document.getElementById('ext-link-overlay');
  const popup = document.getElementById('ext-link-popup');
  overlay.classList.remove('active');
  popup.classList.remove('active');
  // รอ transition จบแล้วค่อยซ่อน display
  setTimeout(() => {
    overlay.style.display = 'none';
  }, 300);
}
function copyExtLink() {
  if (!currentExternalURL) return;
  navigator.clipboard.writeText(currentExternalURL).then(() => {
    const copyBtn = document.querySelector('.btn-copy-inside');
    // เปลี่ยนสี Icon และพื้นหลังชั่วคราว
    const originalContent = copyBtn.innerHTML;
    copyBtn.style.color = "#28a745";
    copyBtn.style.borderColor = "#28a745";
    setTimeout(() => {
      copyBtn.style.color = "#1a1a1a";
      copyBtn.style.borderColor = "#ddd";
    }, 1500);
  });
}
function redirectToExtLink() {
  if (!currentExternalURL) return;
  closeExternalLink();
  try {
    if (window.liff && typeof _liffReady !== 'undefined' && _liffReady) {
      console.log("Opening via LIFF: " + currentExternalURL);
      liff.openWindow({
        url: currentExternalURL,
        external: true // บังคับเปิดด้วย Default Browser ของเครื่อง (เช่น Safari, Chrome)
      });
      return;
    }
  } catch (e) {
    console.warn("LIFF openWindow failed, using fallback.", e);
  }
  // 2. Fallback สำหรับ Browser ทั่วไป
  console.log("Opening via Fallback: " + currentExternalURL);
  // เพิ่ม noopener,noreferrer เพื่อความปลอดภัยจากช่องโหว่ Tabnabbing
  window.open(currentExternalURL, '_blank', 'noopener,noreferrer');
}
















// Copy
let __copyText = "";
let __copyTimeout = null;

function copy(str, x = null, y = null) {
  __copyText = String(str ?? "");
  const bubble = document.getElementById('copyBubble');
  const detector = document.getElementById('copyDetector');
  const canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");

  bubble.style.display = '';

  if (x !== null && y !== null && canvas) {
    const rect = canvas.getBoundingClientRect();
    const scaleX = rect.width / canvas.width;
    const scaleY = rect.height / canvas.height;
    const finalX = (x * scaleX) + rect.left;
    const finalY = rect.top + rect.height - (y * scaleY);

    bubble.style.left = finalX + 'px';
    bubble.style.top = finalY + 'px';
  } else {
    bubble.style.left = '50%'; bubble.style.top = '50%';
  }

  detector.style.display = 'block';
  bubble.classList.add('show');

  if (__copyTimeout) clearTimeout(__copyTimeout);
  __copyTimeout = setTimeout(closeBubbleOnly, 5000);
}

async function confirmCopy() {
  const ta = document.createElement('textarea');
  ta.value = __copyText;
  ta.style.position = 'fixed';
  ta.style.opacity = '0';
  document.body.appendChild(ta);

  ta.focus();
  ta.select();
  ta.setSelectionRange(0, 99999);

  try {
    document.execCommand('copy');
  } catch (err) {
    console.error("Copy failed", err);
  }
  document.body.removeChild(ta);
  closeBubbleOnly();
}

function closeBubbleOnly() {
  const bubble = document.getElementById('copyBubble');
  const detector = document.getElementById('copyDetector');
  bubble.classList.remove('show');
  bubble.style.display = 'none';
  detector.style.display = 'none';
  if (__copyTimeout) clearTimeout(__copyTimeout);
}

async function copyToClipboard(text) {
  try {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return true;
    }
  } catch (err) { }

  try {
    let ta = document.getElementById('__hidden_copy_ta__');
    if (!ta) {
      ta = document.createElement('textarea');
      ta.id = '__hidden_copy_ta__';
      ta.setAttribute('readonly', '');
      ta.style.position = 'fixed';
      ta.style.left = '-9999px';
      ta.style.top = '0';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
    }
    ta.value = text;
    ta.focus();
    ta.select();
    ta.setSelectionRange(0, 99999);
    const ok = document.execCommand('copy');
    window.getSelection().removeAllRanges();
    return ok;
  } catch (err) {
    return false;
  }
}

//save File
function SaveFile(fileName, mode, data) {
  var source = '';
  if (mode == "image") source = 'data:image/png;base64,' + data;
  if (mode == "text") source = 'data:text/plain;base64,' + data;
  if (mode == "tsv") source = 'data:text/tab-separated-values;base64,' + data;
  if (mode == "csv") source = 'data:text/csv;base64,' + data;

  const downloadLink = document.createElement('a');
  document.body.appendChild(downloadLink);
  downloadLink.href = source;
  downloadLink.target = '_self';
  downloadLink.download = fileName;
  downloadLink.click();
  document.body.removeChild(downloadLink);
}

//open Popop Input
function openInput(headerText = "กรุณากรอกข้อความ", placeholderText = "") {
  const overlay = document.getElementById("text-overlay");
  const input = document.getElementById("userInput");
  const header = document.getElementById("popup-header");
  const bg = document.getElementById("overlay-bg");

  header.textContent = headerText;
  input.value = placeholderText;
  overlay.style.display = "block";
  bg.style.display = "block";
  canvas.style.pointerEvents = "none";

  setTimeout(() => {
    input.focus();
  }, 100);

  input.onkeydown = (e) => {
    if (e.key === "Enter") {
      closeInput();
    }
  };
}

function closeInput() {
  const input = document.getElementById("userInput");
  const text = input.value;
  const overlay = document.getElementById("text-overlay");
  const bg = document.getElementById("overlay-bg");

  input.blur();
  setTimeout(() => {
    overlay.style.display = "none";
    bg.style.display = "none";
    canvas.style.pointerEvents = "auto";
    if (typeof m_unityInstance !== 'undefined') {
      m_unityInstance.SendMessage('HtmlCallback', 'Callback', text);
    }
  }, 100);
}

//open Popop
function openPopup(imageSrc) {
  var popup = document.getElementById('popup-container');
  var popupImg = document.getElementById('popup-img');
  popupImg.src = imageSrc;
  popup.style.display = 'block';
}

function closePopup() {
  var popup = document.getElementById('popup-container');
  popup.style.display = 'none';
  m_unityInstance.SendMessage('HtmlCallback', 'Callback', "close-popup-img");
}
function getAssets(fileCode, fillback) {
  if (appData.customAssets[fileCode]) {
    return appData.customAssets[fileCode];
  }
  else if (appData.path[fileCode]) {
    return appData.path[fileCode] + "?v=" + htmlCacheVersion;
  }
  return fillback;
}
//iframe
function openIframe(url, x = 0, y = 0, type = '%', payload = null) {
  const manualsize = (x && y) && (x != 0 && y != 0);
  const overlay = document.getElementById("iframe-overlay");
  const iframe = document.getElementById("custom-iframe");
  const container = document.getElementById("iframe-container");
  const closeBtn = document.getElementById("close-iframe-btn");



  overlay.classList.remove('closing');
  const separator = url.includes('?') ? '&' : '?';
  iframe.src = url + separator + 'v=' + htmlCacheVersion;
  //iframe.src = url + "?v=" + htmlCacheVersion;
  overlay.classList.add('active');

  const isFull = (x == 100 && y == 100 && type === '%');
  if (isFull) container.classList.add('is-full');
  else container.classList.remove('is-full');

  if (payload) {
    iframe.onload = function () {
      iframe.contentWindow.postMessage(
        { type: 'initialData', data: payload }, "*"
      );
    };
  }

  if (!isPortraitScreen && type === '%') {
    container.style.width = (canvas_x * (x / 100)) + 'px';
    container.style.height = (canvas_y * (y / 100)) + 'px';
  } else {
    if (manualsize) {
      container.style.width = type === 'px' ? x + 'px' : x + '%';
      container.style.height = type === 'px' ? y + 'px' : y + '%';
    } else {
      container.style.width = '80%';
      container.style.height = '80%';
    }
  }



  closeBtn.onclick = () => {
    closeIframe();
  }
  // pause game when iframe is open
  m_unityInstance.SendMessage('HtmlCallback', 'FormTimeScale', "0");
}
function updateIframe(payload) {
  const iframe = document.getElementById("custom-iframe");
  if (iframe) {
    iframe.contentWindow.postMessage(
      { type: 'updateData', data: payload }, "*"
    );
  }
}
function callbackIframe(event) {
  const receivedData = event.data.data;
  let jsonString;
  if (typeof receivedData === 'string') jsonString = receivedData;
  else if (typeof receivedData === 'object' && receivedData !== null) jsonString = JSON.stringify(receivedData);
  else return;

  canvas.focus();
  window.focus();

  m_unityInstance.SendMessage('HtmlCallback', 'FormCallback', jsonString);
  if (event.data.closeIframe === true) closeIframe();
}
function onUpdateApp(loading = true) {
  const updateApp = { updateApp: loading ?? false }
  m_unityInstance.SendMessage('HtmlCallback', 'FormCallback', JSON.stringify(updateApp));
}
function onPlaySfxInApp(clipName) {
  const sfxInApp = { sfxInApp: clipName }
  m_unityInstance.SendMessage('HtmlCallback', 'FormCallback', JSON.stringify(sfxInApp));
}
function closeIframe() {
  const overlay = document.getElementById("iframe-overlay");
  const iframe = document.getElementById("custom-iframe");

  if (!overlay || !overlay.classList.contains('active')) return;
  overlay.classList.add('closing');

  // resume game when iframe is closed
  m_unityInstance.SendMessage('HtmlCallback', 'FormTimeScale', "1");

  setTimeout(() => {
    overlay.classList.remove('active');
    overlay.classList.remove('closing');
    iframe.src = "";
    canvas.focus();
    window.focus();
  }, 300);
}
let externalApiName;
let externalApiCallback;
function callExternalApi(functionName, body, callback) {
  externalApiName = functionName;
  externalApiCallback = callback;
  const payload = {
    functionName,
    body
  };
  // เรียก native / SDK
  canvas.focus();
  window.focus();
  m_unityInstance.SendMessage('HtmlCallback', 'OnCallExternalApi', JSON.stringify(payload));
}
function callbackExternalApi(functionName, response) {
  canvas.focus();
  window.focus();
  console.log(`callbackExternalApi : ${functionName}`);
  if (externalApiName == functionName && externalApiCallback) {
    externalApiCallback(response);
  }
  externalApiName = null;
  externalApiCallback = null;
}







//text to UTF8
var toUtf8 = function (text) {
  var surrogate = encodeURIComponent(text);
  var result = '';
  for (var i = 0; i < surrogate.length;) {
    var character = surrogate[i];
    i += 1;
    if (character == '%') {
      var hex = surrogate.substring(i, i += 2);
      if (hex) result += String.fromCharCode(parseInt(hex, 16));
    } else {
      result += character;
    }
  }
  return result;
};

//Share
function Share(title, text, url) {
  if (navigator.share) {
    const shareData = { title: title, text: text, url: url };
    navigator.share(shareData)
      .then(() => console.log('Shared successfully'))
      .catch((error) => console.log('Error sharing:', error));
  } else {
    console.log('Web Share API is not supported');
  }
}

function loadJsQR(callback) {
  if (typeof jsQR === 'undefined') {
    var script = document.createElement('script');
    script.src = '[https://cdn.jsdelivr.net/npm/jsqr/dist/jsQR.js](https://cdn.jsdelivr.net/npm/jsqr/dist/jsQR.js)';
    script.onload = function () {
      console.log('jsQR has been loaded.');
      callback();
    };
    document.body.appendChild(script);
  } else {
    console.log('jsQR is already loaded.');
    callback();
  }
}

function ScaneQRCode(data, x, y, size) {
  var base64String = `data:image/png;base64,${data}`;
  console.log(`ScaneQRCode : 1 `);

  let img = new Image();
  img.src = base64String;
  img.onload = function () {
    console.log("✅ Image Loaded!");
    const canvas = document.createElement("canvas");
    const ctx = canvas.getContext("2d");
    canvas.width = img.width;
    canvas.height = img.height;
    ctx.drawImage(img, 0, 0);

    let imageData = ctx.getImageData(0, 0, img.width, img.height);
    loadJsQR(function () {
      console.log("jsQR start !");
      const code = jsQR(imageData.data, imageData.width, imageData.height);
      if (code) {
        console.log("QR Code:", code.data);
        m_unityInstance.SendMessage('HtmlCallback', 'Callback', code.data);
      } else {
        console.log("ไม่พบ QR Code");
        m_unityInstance.SendMessage('HtmlCallback', 'Callback', null);
      }
    });
  };
  img.onerror = function () { console.error("❌ Error loading image"); };
}

function base64ImageToBlob(str) {
  var pos = str.indexOf(';base64,');
  var type = str.substring(5, pos);
  var b64 = str.substr(pos + 8);
  var imageContent = atob(b64);
  var buffer = new ArrayBuffer(imageContent.length);
  var view = new Uint8Array(buffer);
  for (var n = 0; n < imageContent.length; n++) {
    view[n] = imageContent.charCodeAt(n);
  }
  var blob = new Blob([buffer], { type: type });
  return blob;
}





//Aoto ClearCache
function autoCaching() {
  var current = localStorage.getItem('version');
  if (current != version) {
    localStorage.setItem('version', version);
    console.log(`new version (${version})`);
    clearCache();
  } else {
    console.log(`continue version (${current} == ${version})`);
  }
}

//Clear Memory Cache
function clearCache() {
  var req = indexedDB.deleteDatabase("UnityCache");
  req.onsuccess = function () { console.log("Deleted database successfully"); };
  req.onerror = function () { console.log("Couldn't delete database"); };
  req.onblocked = function () { console.log("Couldn't delete database due to the operation being blocked"); };
}
