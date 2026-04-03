import i18next from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import moment from 'moment';

function loadLocaleMessages() {
    const locales = import.meta.glob('../locale/*.json', { eager: true });

    const messages = {};

    for (const item in locales) {
        const matched = item.match(/([A-Za-z0-9-_]+)\./i);
        if (matched && matched.length > 1) {
            const locale = matched[1];

            messages[locale] = {
                translation: Object.assign(
                    messages[locale] || {},
                    locales[item]
                )
            };
        }
    }
    return messages;
}

const messages = loadLocaleMessages();

const userLang = navigator.language || navigator.userLanguage;
const userLocale = userLang.split('-')[0];

i18next.use(LanguageDetector).init({
    debug: false,
    lng: userLocale || import.meta.env.VUE_APP_I18N_LOCALE || 'en',
    fallbackLng: import.meta.env.VUE_APP_I18N_FALLBACK_LOCALE || 'en',
    /*backend: {
        loadPath: function(languages) {
            var key = 'QuestionnaireEditor.' + languages[0] + '.json';
            return window.localization[key];
        }
    },*/
    load: 'languageOnly',
    useCookie: false,
    useLocalStorage: false,
    resources: messages
});

// i18next v26 removed interpolation.format; register custom formatters via the Formatter API.
// These handle moment.js format strings used in locale keys (e.g. {{dateTime, H:mm}}).
i18next.services.formatter.add('uppercase', (value) => String(value).toUpperCase());

// Dynamically scan all loaded locale values for {{var, format}} patterns and register
// a moment-based formatter for every unique format spec found.  This way adding a new
// format string to any locale file works automatically without touching this file.
// null/undefined are returned as-is, matching the previous interpolation.format fallthrough.
function collectFormats(obj, found = new Set()) {
    if (typeof obj === 'string') {
        const re = /\{\{[^,}]+,\s*([^}]+?)\s*\}\}/g;
        let m;
        while ((m = re.exec(obj)) !== null) found.add(m[1]);
    } else if (obj && typeof obj === 'object') {
        for (const v of Object.values(obj)) collectFormats(v, found);
    }
    return found;
}

const momentFormats = collectFormats(messages);
momentFormats.delete('uppercase'); // handled separately above
momentFormats.forEach((fmt) => {
    i18next.services.formatter.add(fmt, (value) => {
        if (value == null) return value;
        if (moment.isDate(value) || moment.isMoment(value))
            return moment(value).format(fmt);
        return value;
    });
});

const instance = i18next;

export default instance;

export const i18n = instance;
