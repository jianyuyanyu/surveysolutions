export const virtualPath = INTERVIEW_APP_CONFIG.virtualPath
export const imageUploadUri = INTERVIEW_APP_CONFIG.imageUploadUri
export const audioUploadUri = INTERVIEW_APP_CONFIG.audioUploadUri
export const imageGetBase = INTERVIEW_APP_CONFIG.imageGetBase
export const signalrPath = INTERVIEW_APP_CONFIG.signalrPath
export const signalrUrlOverride = INTERVIEW_APP_CONFIG.signalrUrlOverride
export const supportedTransports = ["webSockets", "longPolling"]
export const verboseMode = INTERVIEW_APP_CONFIG.verboseLogging
export const assetsPath = INTERVIEW_APP_CONFIG.assetsPath
export const appVersion = INTERVIEW_APP_CONFIG.appVersion
export const googleApiKey = INTERVIEW_APP_CONFIG.googleApiKey
export const hqLink = INTERVIEW_APP_CONFIG.hqLink
export const locale = INTERVIEW_APP_CONFIG.locale

var navigator = window.navigator;
var lang = navigator.languages ? navigator.languages[0] : (navigator.language || navigator.userLanguage)
export const browserLanguage = lang