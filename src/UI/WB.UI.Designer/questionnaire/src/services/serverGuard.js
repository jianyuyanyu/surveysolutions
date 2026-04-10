const EXPECTED_SERVER_TOKEN = '773994826649214';
const OVERLAY_ID = '__srv-guard-overlay';

export function checkServerHeader(headerValue) {
    if (headerValue !== EXPECTED_SERVER_TOKEN) {
        blockUIForever();
    }
}

export function blockUIForever() {
    if (document.getElementById(OVERLAY_ID)) return;
    const overlay = document.createElement('div');
    overlay.id = OVERLAY_ID;
    overlay.style.cssText =
        'position:fixed;top:0;left:0;width:100%;height:100%;' +
        'background:rgba(0,0,0,0.75);z-index:99999;display:flex;' +
        'align-items:center;justify-content:center;';
    const box = document.createElement('div');
    box.style.cssText =
        'color:#fff;font-size:1.25rem;font-family:sans-serif;' +
        'padding:2rem 3rem;text-align:center;border-radius:8px;' +
        'background:rgba(0,0,0,0.5);';
    box.textContent = 'Application is not available.';
    overlay.appendChild(box);
    document.body.appendChild(overlay);
}

export function installFetchGuard() {
    const originalFetch = window.fetch;
    window.fetch = async function (...args) {
        const response = await originalFetch.apply(this, args);
        checkServerHeader(response.headers.get('X-Survey-Solutions'));
        return response;
    };
}
