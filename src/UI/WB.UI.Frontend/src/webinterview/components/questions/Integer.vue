﻿<template>
    <wb-question :question="$me" questionCssClassName="numeric-question" :no-comments="noComments">
        <div class="question-unit">
            <div class="options-group">
                <div class="form-group" v-if="$me.isProtected">
                    <div class="field locked-option unavailable-option answered">
                        <input type="number" class="field-to-fill" placeholder="Tap to enter number"
                            :value="$me.protectedAnswer" disabled />
                        <button type="submit" class="btn btn-link btn-clear">
                            <span></span>
                        </button>
                        <div class="lock"></div>
                    </div>
                </div>
                <div class="form-group">
                    <div class="field" :class="{ answered: $me.isAnswered }">
                        <input ref="inputInt" type="text" autocomplete="off" inputmode="numeric" class="field-to-fill"
                            :placeholder="noAnswerWatermark" :title="noAnswerWatermark" :value="$me.answer"
                            v-blurOnEnterKey :disabled="isSpecialValueSelected || !$me.acceptAnswer"
                            :class="{ 'special-value-selected': isSpecialValueSelected }" @blur="answerIntegerQuestion"
                            v-numericFormatting="{
                                digitGroupSeparator: groupSeparator,
                                decimalCharacter: decimalSeparator,
                                decimalPlaces: 0,
                                minimumValue: '-2147483648',
                                maximumValue: '2147483647'
                            }" />
                        <wb-remove-answer v-if="!isSpecialValueSelected && !$me.isProtected"
                            :on-remove="removeAnswer" />
                    </div>
                </div>
                <template v-if="isSpecialValueSelected != false">
                    <div class="radio" v-for="option in $me.options" :key="$me.id + '_' + option.value">
                        <div class="field">
                            <input class="wb-radio" type="radio" :id="$me.id + '_' + option.value" :name="$me.id"
                                :value="option.value" :disabled="!$me.acceptAnswer" v-model="specialValue" />
                            <label :for="$me.id + '_' + option.value">
                                <span class="tick"></span>
                                {{ option.title }}
                            </label>
                            <wb-remove-answer :on-remove="removeAnswer" v-if="!$me.isProtected" />
                            <wb-attachment :attachmentName="option.attachmentName" :interviewId="interviewId"
                                v-if="option.attachmentName" />
                        </div>
                    </div>
                </template>
                <wb-lock />
            </div>
        </div>
    </wb-question>
</template>
<script lang="js">
import { entityDetails } from '../mixins'
import modal from '@/shared/modal'
import { getGroupSeparator, getDecimalSeparator } from './question_helpers'

export default {
    data() {
        return {
            //autoNumericElement: null
        }
    },
    name: 'Integer',
    mixins: [entityDetails],
    props: ['noComments'],
    computed: {
        autoNumericElement() {
            return this.$refs.inputInt.autoNumericElement
        },
        isSpecialValueSelected() {
            if (this.$me.answer == null || this.$me.answer == undefined)
                return undefined
            return this.isSpecialValue(this.$me.answer)
        },
        noAnswerWatermark() {
            return !this.$me.acceptAnswer && !this.$me.isAnswered ? this.$t('Details.NoAnswer') : this.$t('WebInterviewUI.NumberEnter')
        },
        groupSeparator() {
            return getGroupSeparator(this.$me)
        },
        decimalSeparator() {
            return getDecimalSeparator(this.$me)
        },
        specialValue: {
            get() {
                return this.$me.answer
            },
            set(value) {
                this.saveAnswer(value, true)
            },
        },
    },
    methods: {
        answerIntegerQuestion(evnt) {
            const answerString = this.autoNumericElement.getNumericString()
            const answer = answerString != undefined && answerString != ''
                ? parseInt(answerString)
                : null

            const isSpecialValue = this.isSpecialValue(answer)
            this.saveAnswer(answer, isSpecialValue)
        },

        saveAnswer(answer, isSpecialValue) {
            this.sendAnswer(() => {
                if (this.handleEmptyAnswer(answer)) {
                    return
                }

                if (answer > 2147483647 || answer < -2147483648 || answer % 1 !== 0) {
                    this.markAnswerAsNotSavedWithMessage(this.$t('WebInterviewUI.NumberCannotParse'), answer)
                    return
                }

                if (this.$me.isProtected && this.$me.protectedAnswer > answer) {
                    this.markAnswerAsNotSavedWithMessage(this.$t('WebInterviewUI.NumberCannotBeLessThanProtected'), answer)
                    return
                }

                if (!this.$me.isRosterSize) {
                    this.$store.dispatch('answerIntegerQuestion', { identity: this.id, answer: answer })
                    return
                }

                if (!isSpecialValue && answer < 0) {
                    this.markAnswerAsNotSavedWithMessage(this.$t('WebInterviewUI.NumberRosterError', { answer }), answer)
                    return
                }

                if (answer > this.$me.answerMaxValue) {
                    this.markAnswerAsNotSavedWithMessage(this.$t('WebInterviewUI.NumberRosterUpperBound', { answer, answerMaxValue: this.$me.answerMaxValue }), answer)
                    return
                }

                const previousAnswer = this.$me.answer
                const isNeedRemoveRosters = previousAnswer != undefined && answer < previousAnswer

                if (!isNeedRemoveRosters) {
                    this.$store.dispatch('answerIntegerQuestion', { identity: this.id, answer: answer })
                    return
                }

                const amountOfRostersToRemove = previousAnswer - Math.max(answer, 0)
                const confirmMessage = this.$t('WebInterviewUI.NumberRosterRemoveConfirm', { amountOfRostersToRemove })

                if (amountOfRostersToRemove > 0) {
                    modal.confirm(confirmMessage, result => {
                        if (result) {
                            this.$store.dispatch('answerIntegerQuestion', { identity: this.id, answer: answer })
                            return
                        } else {
                            this.fetch()
                            if (this.autoNumericElement)
                                this.autoNumericElement.set(this.$me.answer)

                            return
                        }
                    })
                }
                else {
                    this.$store.dispatch('answerIntegerQuestion', { identity: this.id, answer: answer })
                    return
                }
            })
        },

        removeAnswer() {
            if (!this.$me.isAnswered) {
                return
            }

            if (this.autoNumericElement)
                this.autoNumericElement.clear()

            if (!this.$me.isRosterSize) {
                this.$store.dispatch('removeAnswer', this.id)
                return
            }

            const amountOfRostersToRemove = this.$me.answer
            if (amountOfRostersToRemove > 0) {
                const confirmMessage = this.$t('WebInterviewUI.NumberRosterRemoveConfirm', { amountOfRostersToRemove })

                modal.confirm(confirmMessage, result => {
                    if (result) {
                        this.$store.dispatch('removeAnswer', this.id)
                    } else {
                        this.fetch()

                        if (this.autoNumericElement)
                            this.autoNumericElement.set(this.$me.answer)
                    }
                })
            }
            else {
                this.$store.dispatch('removeAnswer', this.id)
            }
        },
        isSpecialValue(value) {
            const options = this.$me.options || []
            if (options.length == 0)
                return false
            for (let i = 0; i < options.length; i++) {
                if (options[i].value === value)
                    return true
            }
            return false
        },
    },
    beforeDestroy() {
        if (this.autoNumericElement) {
            this.autoNumericElement.remove()
        }
    },
}
</script>
