// main entry point to signalr api hub

import * as jQuery from "jquery"
global.$ = global.jQuery = require('jquery');
// tslint:disable-next-line:max-line-length
import { appVersion, audioUploadUri, imageUploadUri, signalrPath, signalrUrlOverride, supportedTransports } from "src/config"

import * as $script from "scriptjs"
import "signalr"
import store from "../store"

// wraps jQuery promises into awaitable ES 2016 Promise
const wrap = (jqueryPromise) => {
    return new Promise((res, rej) =>
        jqueryPromise
            .done(data => res(data))
            .fail(error => rej(error))
    )
}

const scriptIncludedPromise = new Promise(resolve =>
    $script(signalrPath, () => {
        // $.connection.hub.logging = true
        const interviewProxy = $.connection.interview

        interviewProxy.client.reloadInterview = () => {
            store.dispatch("reloadInterview")
        }

        interviewProxy.client.closeInterview = () => {
            store.dispatch("closeInterview")
            store.dispatch("stop")
        }

        interviewProxy.client.finishInterview = () => {
            store.dispatch("finishInterview")
        }

        interviewProxy.client.refreshEntities = (questions) => {
            store.dispatch("refreshEntities", questions)
        }

        interviewProxy.client.refreshComment = (question) => {
            store.dispatch("refreshComment", question)
        }

        interviewProxy.client.refreshSection = () => {
            store.dispatch("fetchSectionEntities")          // fetching entities in section
            store.dispatch("refreshSectionState")           // fetching breadcrumbs/sidebar/buttons
        }

        interviewProxy.client.markAnswerAsNotSaved = (id, message) => {
            store.dispatch("fetchProgress", -1)
            store.dispatch("fetch", { id, done: true })
            store.dispatch("setAnswerAsNotSaved", { id, message })
        }

        interviewProxy.server.answerPictureQuestion = (id, file) => {
            const fd = new FormData()
            fd.append("interviewId", queryString.interviewId)
            fd.append("questionId", id)
            fd.append("file", file)
            store.dispatch("uploadProgress", { id, now: 0, total: 100 })

            return $.ajax({
                url: imageUploadUri,
                xhr() {
                    const xhr = $.ajaxSettings.xhr()
                    xhr.upload.onprogress = (e) => {
                        store.dispatch("uploadProgress", {
                            id,
                            now: e.loaded,
                            total: e.total
                        })
                    }
                    return xhr
                },
                data: fd,
                processData: false,
                contentType: false,
                type: "POST"
            })
        }

        interviewProxy.server.answerAudioQuestion = (id, file) => {
            const fd = new FormData()
            fd.append("interviewId", queryString.interviewId)
            fd.append("questionId", id)
            fd.append("file", file)
            store.dispatch("uploadProgress", { id, now: 0, total: 100 })

            return $.ajax({
                url: audioUploadUri,
                xhr() {
                    const xhr = $.ajaxSettings.xhr()
                    xhr.upload.onprogress = (e) => {
                        store.dispatch("uploadProgress", {
                            id,
                            now: e.loaded,
                            total: e.total
                        })
                    }
                    return xhr
                },
                data: fd,
                processData: false,
                contentType: false,
                type: "POST"
            })
        }

        resolve()
    })
)

async function hubStarter() {
    if (signalrUrlOverride) {
        $.connection.hub.url = signalrUrlOverride
    }

    $.connection.hub.qs = queryString

    // { transport: supportedTransports }
    await wrap($.signalR.hub.start())
    // await wrap($.signalR.hub.start({ transport: "longPolling" }))
    $.connection.hub.connectionSlow(() => {
        store.dispatch("connectionSlow")
    })

    $.connection.hub.reconnecting(() => {
        store.dispatch("tryingToReconnect", true)
    })

    $.connection.hub.reconnected(() => {
        store.dispatch("tryingToReconnect", false)
    })

    $.connection.hub.disconnected(() => {
        store.dispatch("disconnected")
    })
}

export const queryString = {
    interviewId: null,
    appVersion
}

export async function getInstance() {
    await scriptIncludedPromise
    await hubStarter()
    return jQuery.signalR.interview
}

async function getInterviewHub() {
    return (await getInstance()).server
}

export async function apiCallerAndFetch(id, action) {
    if (id) {
        store.dispatch("fetch", { id })
    }
    const hub = await getInterviewHub()

    store.dispatch("fetchProgress", 1)

    try {
        return await wrap(action(hub))
    } catch (err) {
    if (id) {
            store.dispatch("setAnswerAsNotSaved", { id, message: err.statusText })
            store.dispatch("fetch", { id, done: true })
        } else {
            store.dispatch("UNHANDLED_ERROR", err)
        }
    } finally {
        store.dispatch("fetchProgress", -1)
    }
}

export async function apiCaller(action) {
    const hub = await getInterviewHub()

    store.dispatch("fetchProgress", 1)

    try {
        return await wrap(action(hub))
    } catch (err) {
        store.dispatch("UNHANDLED_ERROR", err)
    } finally {
        store.dispatch("fetchProgress", -1)
    }
}

export function apiStop() {
    $.connection.hub.stop()
}