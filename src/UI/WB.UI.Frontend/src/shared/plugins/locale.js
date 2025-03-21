import i18next from 'i18next'

let t

export default {
    initialize(browserLanguage, vue) {
        const locale = browserLanguage.split('-')[0]

        const options = {
            lng: locale,
            nsSeparator: '.',
            keySeparator: ':',
            fallbackLocale: 'en',
            resources: {
                [locale]: window.CONFIG.locale.data,
            },
            interpolation: { escapeValue: false, skipOnVariables: false },
            saveMissing: true,
            missingKeyHandler(lng, ns, key, fallbackValue) {
                console.warn('Missing translation for language', lng, 'key', ns + '.' + fallbackValue)
            },
            appendNamespaceToMissingKey: true,
            parseMissingKeyHandler(key) {
                return '[' + key + ']'
            },
        }
        i18next.init(options)

        vue.config.globalProperties.$t = function () {
            return i18next.t.apply(i18next, arguments)
        }

        t = vue.config.globalProperties.$t
        return i18next
    },
}

export function $t(...args) {
    return t(...args)
}

