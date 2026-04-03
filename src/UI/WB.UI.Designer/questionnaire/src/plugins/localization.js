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

// Register a moment-based formatter for every format spec found in locale files.
// null/undefined are returned as-is, matching the previous interpolation.format fallthrough.
function addMomentFormatter(fmt) {
    i18next.services.formatter.add(fmt, (value) => {
        if (value == null) return value;
        if (moment.isDate(value) || moment.isMoment(value))
            return moment(value).format(fmt);
        return value;
    });
}
addMomentFormatter('H:mm');
addMomentFormatter('H: mm');
addMomentFormatter('MMMM DD[,] YYYY [at] k:mm');
addMomentFormatter('MMMM DD[,] YYYY [в] k:mm');
addMomentFormatter('MMMM DD [,] YYYY [la] k:mm');

const instance = i18next;

export default instance;

export const i18n = instance;
