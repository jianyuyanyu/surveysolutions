import fg from 'fast-glob';
import fs from 'fs'
import path from 'path'
import crypto from 'crypto'
import xmldoc from 'xmldoc'
import { rimrafSync } from 'rimraf'

module.exports = class LocalizationBuilder {
    constructor(options) {
        this.options = options || {
            patterns: ['../**/*.resx'],
            destination: './locale/.resources'
        };

        this.localeInfo = null;
        this.files = null;
    }

    prepareLocalizationFiles(compilation) {
        this.parseResxFiles(compilation);

        const locales = this.options.locales;
        Object.keys(locales).forEach(page => {
            // eslint-disable-next-line no-prototype-builtins
            if (locales.hasOwnProperty(page))
                this.writeFiles(
                    this.options.destination,
                    path.join('.', page),
                    locales[page]
                );
        });
    }

    writeFiles(destination, folder, namespaces) {
        const response = {};
        const destinationFolder = path.join(destination, folder);
        rimrafSync(destinationFolder);

        Object.keys(this.localeInfo).forEach(language => {
            const locale = this.localeInfo[language];
            const content = {};

            namespaces.forEach(ns => {
                if (locale[ns]) content[ns] = locale[ns];
            });

            const fileBody = this.options.inline
                ? `${JSON.stringify(content, null, 2)}`
                : `__setLocaleData__(${JSON.stringify(content)})`;

            const hash = this.options.noHash
                ? ''
                : '.' + this.getHash(fileBody);

            const filename = language + hash + '.js';

            const resultPath = path.join(destinationFolder, filename);

            this.ensureDirectoryExistence(resultPath);

            fs.writeFileSync(resultPath, fileBody);

            //console.log('Localization: generated: ', resultPath)

            response[language] = path
                .join(folder, filename)
                .replace(/\\/g, '/');
        });

        return response;
    }

    getFiles() {
        const { patterns } = this.options;
        let files = fg.sync(patterns, { onlyFiles: true });
        if (files.length === 0)
            throw 'None of resource files (.resx) were found.';

        return files;
    }

    parseResxFiles() {
        console.time('parseResxFiles');
        var files = this.getFiles();

        const locale = {}; /* en: { Namespace: { key: "sdfsdf" } } */

        for (let index = 0; index < files.length; index++) {
            const file = files[index];

            const xml = fs.readFileSync(file);

            const info = path.parse(file);

            if (!info.name.includes('.')) {
                info.name += '.en';
            }

            const json = this.doConvert(xml);

            const { lang, namespace } = this.parseFilename(info.name);

            if (locale[lang] == null) {
                locale[lang] = {};
            }

            const translations = locale[lang];

            if (translations[namespace] == null) {
                translations[namespace] = {};
            }

            translations[namespace] = this.mergeNamespaces(
                namespace,
                translations[namespace],
                json
            );
        }

        this.addDefaultLocaleValues(locale, 'en');

        this.localeInfo = locale;
        console.timeEnd('parseResxFiles');
        return this.localeInfo;
    }

    addDefaultLocaleValues(locales, def) {
        const defaultMessages = locales[def];

        // translations: en, ru, es
        Object.keys(locales).forEach(locale => {
            if (locale == def) return;

            // namespaces: Main, DataTables
            Object.keys(defaultMessages).forEach(namespace => {
                if (!locales[locale][namespace]) {
                    locales[locale][namespace] = {};
                }
                // key: Version, ModalTitle
                Object.keys(defaultMessages[namespace]).forEach(key => {
                    if (!locales[locale][namespace][key]) {
                        locales[locale][namespace][key] =
                            defaultMessages[namespace][key];
                    }
                });
            });
        });

        return locales;
    }

    getDictionaryDefinition(locales) {
        var result = Object.keys(locales).map(
            locKey => `{ "${locKey}", "${locales[locKey]}" }`
        );

        return result.join(',');
    }

    // Convert XML to JSON
    doConvert(xml) {
        var doc = new xmldoc.XmlDocument(xml);

        var resourceObject = {};
        var valueNodes = doc.childrenNamed('data');
        valueNodes.forEach(function (element) {
            var name = element.attr.name;
            var values = element.childrenNamed('value');

            if (values.length == 1) {
                resourceObject[name] = values[0].val;
            }
        });

        return resourceObject;
    }

    parseFilename(filename) {
        const split = filename.split('.');
        return {
            namespace: split[0],
            lang: split[1]
        };
    }

    mergeNamespaces(namespace, initial, newone) {
        // Object.keys(initial).forEach(key => {
        //     // if (newone[key] != null) {
        //     //     console.error(
        //     //         "Found conflicting resources with same resource name",
        //     //         namespace,
        //     //         key,
        //     //         initial[key],
        //     //         newone[key]
        //     //     );
        //     // }
        // });

        var result = Object.assign(initial || {}, newone);
        return result;
    }

    getHash(content) {
        return crypto
            .createHash('sha1')
            .update(content)
            .digest('hex')
            .substring(0, 12);
    }

    ensureDirectoryExistence(filePath) {
        var dirname = path.dirname(filePath);

        if (fs.existsSync(dirname)) {
            return true;
        }
        this.ensureDirectoryExistence(dirname);
        fs.mkdirSync(dirname);
    }
};
