<template>
    <main class="web-interview web-interview-for-supervisor">
        <div class="container-fluid">
            <div class="row">
                <div v-if="!isLoaded" class="loading">
                    <div style="margin-top:90px">
                        {{ $t("WebInterviewUI.LoadingWait") }}
                    </div>
                </div>
                <Form as="div" v-slot="{ errors, meta }" ref="createForm" class="unit-section complete-section" v-else>
                    <div class="wrapper-info error">
                        <div class="container-info">
                            <h2>
                                {{ $t('Assignments.CreatingNewAssignment', { questionnaire: questionnaireTitle }) }}
                                <span :title="$t('Reports.Version')">({{ this.$t('Assignments.QuestionnaireVersion', {
                                    version: this.questionnaireVersion
                                }) }})</span>
                            </h2>
                        </div>
                    </div>
                    <component v-for="entity in entities" :key="`${entity.identity}-${entity.entityType}`"
                        :is="entity.entityType" :id="entity.identity" fetchOnMount noComments="true"></component>
                    <wb-question ref="ref_newResponsibleId" :question="assignToQuestion" noValidation="true"
                        :noComments="true" :no-title="false" questionCssClassName="single-select-question">
                        <h5>{{ $t("Assignments.CreateAssignment_ResponsibleInstruction") }}</h5>
                        <div class="question-unit">
                            <div class="options-group">
                                <div class="form-group">
                                    <div class="field" :class="{ answered: newResponsibleId != null }">
                                        <Field v-slot="{ field }" label="Responsible" name="newResponsibleId"
                                            :value="newResponsibleId" :rules="responsibleValidations">
                                            <Typeahead v-bind="field" control-id="newResponsibleId"
                                                :placeholder="$t('Common.Responsible')" :value="newResponsibleId"
                                                :ajax-params="{}" @selected="newResponsibleSelected"
                                                :fetch-url="config.responsiblesUrl"></Typeahead>
                                        </Field>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="information-block text-danger" v-if="!assignToQuestion.validity.isValid">
                            <p>{{ errors.newResponsibleId }}</p>
                        </div>
                    </wb-question>

                    <wb-question ref="ref_size" :question="sizeQuestion" noValidation="true" noComments="true"
                        questionCssClassName="numeric-question">
                        <h5>{{ this.$t("Assignments.Expected") }}</h5>
                        <div class="instructions-wrapper">
                            <div class="information-block instruction">
                                <p>{{ this.$t("Assignments.ExpectedInstructions") }}</p>
                            </div>
                        </div>
                        <div class="question-unit">
                            <div class="options-group">
                                <div class="form-group">
                                    <div class="field answered">
                                        <Field v-model="sizeQuestion.answer"
                                            :title="this.$t('Assignments.ExpectedExplanation')" :rules="sizeValidations"
                                            name="size" maxlength="5" type="text" autocomplete="off" inputmode="numeric"
                                            class="field-to-fill" />
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="information-block text-danger" v-if="!sizeQuestion.validity.isValid">
                            <!--p>{{ this.$t("Assignments.InvalidExpected") }}</p-->
                            <p>{{ errors.size }}</p>
                        </div>
                    </wb-question>

                    <wb-question :question="webMode" noValidation="true" noComments="true"
                        questionCssClassName="multiselect-question">
                        <h5>{{ this.$t("Assignments.WebMode") }} <a target="_blank"
                                href="https://support.mysurvey.solutions/headquarters/cawi">
                                (?)
                            </a></h5>
                        <div class="question-unit">
                            <div class="options-group">
                                <div class="form-group">
                                    <div class="field answered">
                                        <input id="webModeId" @change="webModeChange" checked="checked"
                                            v-model="webMode.answer" data-val="true" type="checkbox"
                                            class="wb-checkbox">
                                        <label for="webModeId">
                                            <span class="tick"></span>
                                            {{ $t("Assignments.CawiActivated") }}
                                        </label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </wb-question>

                    <wb-question ref="ref_email" :question="emailQuestion" noValidation="true" noComments="true"
                        :isDisabled="!webMode.answer" questionCssClassName="text-question">
                        <h5>{{ this.$t("Assignments.Email") }}</h5>
                        <div class="question-unit">
                            <div class="options-group">
                                <div class="form-group">
                                    <div class="field answered">
                                        <Field v-model="emailQuestion.answer"
                                            :title="this.$t('Assignments.EmailExplanation')"
                                            :placeholder="$t('Assignments.EnterEmail')" rules="email" name="email"
                                            type="text" autocomplete="off" class="field-to-fill" />
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="information-block text-danger" v-if="!emailQuestion.validity.isValid">
                            <p>{{ this.$t("Assignments.InvalidEmail") }}</p>
                        </div>
                    </wb-question>

                    <wb-question ref="ref_password" :question="passwordQuestion" noValidation="true" noComments="true"
                        :isDisabled="!webMode.answer" questionCssClassName="text-question">
                        <h5>{{ this.$t("Assignments.Password") }}</h5>
                        <div class="instructions-wrapper">
                            <div class="information-block instruction">
                                <p>{{ this.$t("Assignments.PasswordInstructions") }}</p>
                            </div>
                        </div>
                        <div class="question-unit">
                            <div class="options-group">
                                <div class="form-group">
                                    <div class="field answered">
                                        <Field v-model="passwordQuestion.answer"
                                            :placeholder="$t('Assignments.EnterPassword')"
                                            :title="this.$t('Assignments.PasswordExplanation')"
                                            :rules="passwordValidations" name="password" type="text" autocomplete="off"
                                            class="field-to-fill" />
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="information-block text-danger" v-if="!passwordQuestion.validity.isValid">
                            <p>{{ this.$t("Assignments.InvalidPassword") }}</p>
                        </div>
                    </wb-question>

                    <wb-question :question="isAudioRecordingEnabled" noValidation="true" noComments="true"
                        :isDisabled="webMode.answer" questionCssClassName="multiselect-question">
                        <h5>{{ this.$t("Assignments.IsAudioRecordingEnabled") }} <a target="_blank"
                                href="https://support.mysurvey.solutions/headquarters/audio-audit/">
                                (?)
                            </a></h5>
                        <div class="question-unit">
                            <div class="options-group">
                                <div class="form-group">
                                    <div class="field answered">
                                        <input id="isAudioRecordingEnabledId" checked="checked"
                                            v-model="isAudioRecordingEnabled.answer" data-val="true" type="checkbox"
                                            class="wb-checkbox">
                                        <label for="isAudioRecordingEnabledId">
                                            <span class="tick"></span>
                                            {{ $t("Assignments.Activated") }}
                                        </label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </wb-question>

                    <wb-question ref="ref_email" :question="targetAreaQuestion" noValidation="true" noComments="true"
                        questionCssClassName="text-question">
                        <h5>{{ this.$t("Assignments.TargetArea") }}</h5>
                        <div class="question-unit">
                            <div class="options-group">
                                <div class="form-group">
                                    <div class="field answered">
                                        <Field v-model="targetAreaQuestion.answer"
                                            :title="this.$t('Assignments.TargetAreaExplanation')"
                                            :placeholder="$t('Assignments.EnterTargetArea')" name="targetArea"
                                            type="text" autocomplete="off" class="field-to-fill" />
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="information-block text-danger" v-if="!targetAreaQuestion.validity.isValid">
                            <p>{{ this.$t("Assignments.InvalidTargetArea") }}</p>
                        </div>
                    </wb-question>


                    <wb-question :question="commentsQuestion" noValidation="true" noComments="true"
                        questionCssClassName="text-question">
                        <h5>{{ this.$t("Assignments.Comments") }}</h5>
                        <div class="question-unit">
                            <div class="options-group">
                                <div class="form-group">
                                    <div class="field answered">
                                        <textarea v-model="commentsQuestion.answer"
                                            :placeholder="$t('Assignments.EnterComments')" name="comments" rows="6"
                                            maxlength="500" class="form-control" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </wb-question>

                    <div class="action-container">
                        <button :class="{ 'shake': buttonAnimated }" type="button" @click="create($event)"
                            class="btn btn-success btn-lg">{{ $t('Common.Create') }}</button>
                    </div>
                </Form>
            </div>
        </div>
        <portal-target name="body" multiple>
        </portal-target>
        <IdleTimeoutService />
        <signalr @connected="connected" mode="takeNew" :interviewId="interviewId" />
    </main>
</template>

<style scoped>
.shake {
    animation: shake 0.82s cubic-bezier(.36, .07, .19, .97) both;
    transform: translate3d(0, 0, 0);
}

@keyframes shake {

    10%,
    90% {
        transform: translate3d(-1px, 0, 0);
    }

    20%,
    80% {
        transform: translate3d(2px, 0, 0);
    }

    30%,
    50%,
    70% {
        transform: translate3d(-4px, 0, 0);
    }

    40%,
    60% {
        transform: translate3d(4px, 0, 0);
    }
}
</style>

<script>
import { nextTick } from 'vue'
import * as toastr from 'toastr'
import http from '~/webinterview/api/http'
import { RoleNames } from '~/shared/constants'
import { filter } from 'lodash'
import '@/assets/css/markup-web-interview.scss'
import { defineAsyncComponent } from 'vue';
import { Form, Field, ErrorMessage } from 'vee-validate'

const validationTranslations = {
    custom: {
        newResponsibleId: {
            required: () => $t('Assignments.ResponsibleRequired'),
        },
    },
}

export default {
    components: {
        Form,
        Field,
        ErrorMessage,
        signalr: defineAsyncComponent(() => import('~/webinterview/components/signalr/core.signalr')),
    },
    data() {
        return {
            buttonAnimated: false,
            assignToQuestion: {
                id: 'assignTo',
                acceptAnswer: true,
                isAnswered: false,
                validity: {
                    isValid: true,
                },
            },
            sizeQuestion: {
                id: 'size',
                acceptAnswer: true,
                isAnswered: true,
                answer: '1',
                validity: {
                    isValid: true,
                },
            },
            newResponsibleId: null,

            emailQuestion: {
                id: 'email',
                acceptAnswer: true,
                isAnswered: false,
                answer: null,
                validity: {
                    isValid: true,
                },
            },
            passwordQuestion: {
                id: 'password',
                acceptAnswer: true,
                isAnswered: false,
                answer: null,
                validity: {
                    isValid: true,
                },
            },
            webMode: {
                id: 'webMode',
                acceptAnswer: true,
                isAnswered: true,
                answer: false,
                validity: {
                    isValid: true,
                },
            },
            isAudioRecordingEnabled: {
                id: 'isAudioRecordingEnabled',
                acceptAnswer: true,
                isAnswered: true,
                answer: false,
                validity: {
                    isValid: true,
                },
            },
            commentsQuestion: {
                id: 'comments',
                acceptAnswer: true,
                isAnswered: true,
                answer: null,
                validity: {
                    isValid: true,
                },
            },
            targetAreaQuestion: {
                id: 'targetArea',
                acceptAnswer: true,
                isAnswered: true,
                answer: null,
                validity: {
                    isValid: true,
                },
            },
        }
    },
    computed: {
        sizeValidations() {
            let validations = {
                regex: {
                    regex: /^-?([0-9]+)$/
                },
                min_value: {
                    min: -1
                },
                max_value: {
                    max: this.config.maxInterviewsByAssignment
                }
            };

            if (this.webMode.answer) {
                if (this.sizeQuestion.answer === '1') {
                    validations.callLocalMethod = {
                        method: this.emailOrPasswordRequired
                    };
                } else {
                    validations.callLocalMethod = {
                        method: this.emailShouldBeEmpty
                    };
                }
            }

            return validations
        },
        responsibleValidations() {
            return {
                required: true,
                callLocalMethod: { method: this.responsibleShouldBeInterviewer },
            }
        },
        passwordValidations() {
            return {
                regex: /^([0-9A-Z]{6,})$|^(\?)$/,
            }
        },
        entities() {
            var filteredSectionData = filter(this.$store.state.webinterview.entities, d => d.identity != 'NavigationButton')
            return filteredSectionData
        },
        isLoaded() {
            return this.$store.state.takeNew.isLoaded
        },
        questionnaireTitle() {
            return this.$store.state.takeNew.interview.questionnaireTitle
        },
        questionnaireVersion() {
            return this.$store.state.takeNew.interview.questionnaireVersion
        },
        config() {
            return this.$config.model
        },
        responsibleId() {
            return this.newResponsibleId != null ? this.newResponsibleId.key : null
        },

        interviewId() {
            return this.config.id
        },
    },

    methods: {
        onResize() {
            var screenWidth = document.documentElement.clientWidth
            this.$store.dispatch('screenWidthChanged', screenWidth)
        },
        newResponsibleSelected(newValue) {
            this.newResponsibleId = newValue
            this.assignToQuestion.isAnswered = this.newResponsibleId != null
            this.assignToQuestion.validity.isValid = this.newResponsibleId != null
        },
        async create(evnt) {
            evnt.target.disabled = true
            const validationResult = await this.$refs.createForm.validate()
            const self = this
            this.sizeQuestion.validity.isValid = !validationResult.errors.size
            this.emailQuestion.validity.isValid = !validationResult.errors.email
            this.passwordQuestion.validity.isValid = !validationResult.errors.password
            this.assignToQuestion.validity.isValid = !validationResult.errors.newResponsibleId

            const submitAllowed = validationResult.valid
            if (submitAllowed) {
                this.$http
                    .post(this.config.createNewAssignmentUrl, {
                        interviewId: this.interviewId,
                        responsibleId: this.responsibleId,
                        quantity: this.sizeQuestion.answer,
                        email: this.emailQuestion.answer,
                        password: this.passwordQuestion.answer,
                        webMode: this.webMode.answer,
                        isAudioRecordingEnabled: this.isAudioRecordingEnabled.answer,
                        comments: this.commentsQuestion.answer,
                        targetArea: this.targetAreaQuestion.answer,
                    })
                    .then(response => {
                        window.location.href = self.config.assignmentsUrl
                    })
                    .catch(e => {
                        if (e.response.data.message) toastr.error(e.response.data.message)
                        else if (e.response.data.ExceptionMessage) toastr.error(e.response.data.ExceptionMessage)
                        else toastr.error(self.$t('Pages.GlobalSettings_UnhandledExceptionMessage'))
                    })
            }
            else {
                evnt.target.disabled = false
                self.buttonAnimated = true

                setTimeout(() => {
                    self.buttonAnimated = false

                    const firstField = Object.keys(validationResult.errors)[0]

                    self.$nextTick(() => {
                        var elToScroll = self.$refs[`ref_${firstField}`]
                        if (elToScroll)
                            elToScroll.$el.scrollIntoView()
                        return
                    })

                }, 1000)
            }
        },

        webModeChange() {
            if (this.webMode.answer == false) {
                this.passwordQuestion.answer = null
                this.emailQuestion.answer = null
                this.passwordQuestion.validity.isValid = true
                this.emailQuestion.validity.isValid = true
            } else if (this.webMode.answer == true) {
                this.isAudioRecordingEnabled.answer = null
            }
        },

        connected() {
            this.$store.dispatch('loadTakeNew', { interviewId: this.interviewId })
        },

        emailOrPasswordRequired() {
            const email = this.emailQuestion.answer;
            const password = this.passwordQuestion.answer;
            const isValid = (email !== null && email !== '') || (password !== null && password !== '')

            if (isValid)
                return true;

            return this.$t('Assignments.ExpectedForWebMode')
        },

        emailShouldBeEmpty() {
            const email = this.emailQuestion.answer;
            const isValid = email === null || email === ''

            if (isValid)
                return true;

            return this.$t('Assignments.InvalidExpectedWithEmail')
        },

        responsibleShouldBeInterviewer() {
            if (!this.webMode.answer)
                return true

            const value = this.newResponsibleId
            const isValid = value.iconClass.toLowerCase() == RoleNames.INTERVIEWER.toLowerCase()
            if (isValid)
                return true;
            return this.$t('Assignments.WebModeNonInterviewer')
        },
    },

    mounted() {
        const self = this

        this.$nextTick(function () {
            window.addEventListener('resize', self.onResize)
            self.onResize()
        })
    },

    updated() {
        nextTick(() => {
            window.ajustNoticeHeight()
            window.ajustDetailsPanelHeight()
        })
    },

    beforeMount() {
        http.install(this, { store: this.$store })
    },

    unmounted() {
        window.removeEventListener('resize', this.onResize)
    },
}
</script>
