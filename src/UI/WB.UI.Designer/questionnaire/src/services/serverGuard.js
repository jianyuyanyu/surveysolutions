import i18next from 'i18next';

const EXPECTED_SERVER_TOKEN = '773994826649214';
const OVERLAY_ID = '__srv-guard-overlay';
const FALLBACK_MESSAGE = 'Unable to complete the request. The response received does not appear to come from the Survey Solutions server.\nThis is usually caused by a network proxy, firewall, or security gateway intercepting the connection.\nPlease check your network connection or contact your IT support.';

function getMessage() {
    if (i18next.isInitialized) {
        return i18next.t('QuestionnaireEditor.ApplicationNotAvailable', { defaultValue: FALLBACK_MESSAGE });
    }
    return FALLBACK_MESSAGE;
}

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
        'background:rgba(0,0,0,0.5);white-space:pre-line;max-width:600px;';
    box.textContent = getMessage();
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

export function installPageGuard() {
    // Capture native fetch before any patching so the ping is always independent.
    const nativeFetch = window.fetch;
    const run = () => {
        nativeFetch(window.location.href, { method: 'HEAD' })
            .then(response => checkServerHeader(response.headers.get('X-Survey-Solutions')))
            .catch(() => { /* network error — do not block */ });
    };
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', run, { once: true });
    } else {
        run();
    }
}
