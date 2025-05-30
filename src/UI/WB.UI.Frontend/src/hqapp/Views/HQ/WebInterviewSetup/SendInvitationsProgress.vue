<template>
    <HqLayout :hasRow="false" :fixedWidth="true" :title="$t('WebInterviewSetup.WebInterviewSetup_Title')">
        <template v-slot:headers>
            <div>
                <ol class="breadcrumb">
                    <li>
                        <a :href="$config.model.api.surveySetupUrl">{{ $t('MainMenu.SurveySetup') }}</a>
                    </li>
                </ol>
                <h1>
                    {{ $t('WebInterviewSetup.WebInterviewSetup_SendInvitationsTitle') }}
                </h1>
            </div>
        </template>
        <div class="row">
            <div class="col-sm-8">
                <h2>{{ title }}</h2>
            </div>
        </div>
        <div class="row">
            <div class="col-sm-7 col-xs-12 action-block preloading-done-with-errors active-preloading">
                <div class="import-progress">

                </div>
            </div>
        </div>
        <div class="row">
            <div class="col-sm-7 col-xs-12">
                <div class="import-progress">
                    <p v-if="isQueued" class="default-text">{{ $t('DataExport.Preparing') }}</p>
                    <p v-if="isInProgress">{{ $t('WebInterviewSettings.Sending') }}</p>
                    <p v-if="isDone">{{ $t('BatchUpload.Finished') }}</p>
                    <p v-if="isFailed" class="error-text">{{ $t('WebInterviewSettings.SendingFailed') }} </p>
                    <p v-if="isCanceled" class="error-text">{{ $t('WebInterviewSettings.ProcessCancelled') }}</p>
                    <p v-if="isInProgress || isQueued">{{ $t('WebInterviewSettings.TotalSent', {
                        totalCount: totalCount
                    })
                        }}
                    </p>
                    <p v-if="processedCount == 0" class="default-text">{{ $t('WebInterviewSettings.NothingWasSent') }}
                    </p>
                    <p v-else class="success-text">
                        {{ $t('WebInterviewSettings.TotalProcessed', { processedCount: processedCount }) }}
                    </p>
                    <p v-if="withErrorsCount == 0" class="default-text">
                        {{ $t('WebInterviewSettings.NoErrors') }}
                    </p>
                    <p v-else class="error-text">
                        {{ $t('WebInterviewSettings.TotalWithErrors', { withErrorsCount: withErrorsCount }) }}
                    </p>
                    <p v-if="isStopped && withErrorsCount > 0">
                        <a :href="exportErrorsLink" target="_blank">{{ $t('WebInterviewSettings.DownloadErrors') }}</a>
                    </p>
                </div>
                <div class="cancelable-progress" v-if="isInProgress">
                    <div class="progress">
                        <div class="progress-bar" role="progressbar" aria-valuenow="60" aria-valuemin="0"
                            aria-valuemax="100" v-bind:style="{ width: overallProgressPercent + '%' }">
                            <span class="sr-only">{{ overallProgressPercent }}%</span>
                        </div>
                    </div>
                    <button class="btn  btn-link" type="button" @click="cancel">
                        {{ $t('Common.Cancel') }}
                    </button>
                </div>
                <div class="action-buttons">
                    <a :href="$config.model.api.surveySetupUrl" class="back-link">
                        {{ $t('WebInterviewSetup.BackToQuestionnaires') }}
                    </a>
                </div>
            </div>
        </div>
    </HqLayout>
</template>
<script>

export default {
    data() {
        return {
            timerId: null,
            title: null,
            questionnaireIdentity: null,
            responsibleName: null,
            processedCount: null,
            withErrorsCount: null,
            totalCount: null,
            status: null,
        }
    },
    created() {
        this.updateStatus()
        this.timerId = window.setInterval(() => {
            this.updateStatus()
        }, 3000)
    },
    computed: {
        isQueued() { return this.status === 'Queued' },
        isInProgress() { return this.status === 'InProgress' },
        isDone() { return this.status === 'Done' },
        isFailed() { return this.status === 'Failed' },
        isCanceled() { return this.status === 'Canceled' },
        isStopped() { return this.isDone || this.isFailed || this.isCanceled },
        overallProgressPercent() {
            return Math.round((this.processedCount * 100) / this.totalCount)
        },
        exportErrorsLink() {
            return this.$config.model.api.exportErrors
        },
    },
    methods: {
        updateStatus() {
            var self = this
            this.$http.get(this.$config.model.api.statusUrl)
                .then(function (response) {
                    const invitationsInfo = response.data || {}
                    self.title = self.$t('Pages.QuestionnaireNameFormat', { name: invitationsInfo.title, version: invitationsInfo.version })
                    self.questionnaireIdentity = invitationsInfo.questionnaireIdentity
                    const status = invitationsInfo.status
                    self.responsibleName = status.responsibleName
                    self.processedCount = status.processedCount || 0
                    self.withErrorsCount = status.withErrorsCount || 0
                    self.totalCount = status.totalCount || 0
                    self.status = status.status
                })
                .catch(function (error) {
                    self.$errorHandler(error, self)
                })
        },
        cancel() {
            var self = this
            self.$store.dispatch('showProgress')
            this.$http.post(this.$config.model.api.cancelUrl).then(function (response) {
                self.$store.dispatch('hideProgress')
            })
        },
    },

}
</script>