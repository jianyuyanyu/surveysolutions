<template>
    <HqLayout :hasFilter="false" :fixedWidth="true" :has-row="false">
        <template v-slot:headers>
            <div>
                <ol class="breadcrumb">
                    <li>
                        <a href="../../SurveySetup">{{ $t('MainMenu.SurveySetup') }}</a>
                    </li>
                </ol>
                <h1>{{ $t('QuestionnaireClonning.CloneQuestionnaireTitle') }}</h1>
            </div>
        </template>

        <div class="row">
            <div class="col-sm-8">
                <h2>
                    {{ $t('QuestionnaireClonning.ToCloneQuestionnaire') }}
                    <b>{{ $t('Pages.QuestionnaireNameFormat', {
                        name: this.$config.model.originalTitle, version:
                            this.$config.model.version}) }}</b>
                </h2>
            </div>
        </div>
        <div class="row">
            <form method="POST" class="col-sm-8" @submit.prevent="tryClone" novalidate ref="frmClone">
                <div class="form-group" :class="{ 'has-error': errors.has('NewTitle') }">
                    <label for="NewTitle" class="control-label">
                        {{ $t('FieldsAndValidations.CloneQuestionnaireModel_NewTitle_Label') }}
                    </label>
                    <input type="text" id="NewTitle" name="NewTitle" v-validate="'required'" class="form-control"
                        autocomplete="off" />
                    <span v-if="errors.has('NewTitle')" class="help-block field-validation-error">
                        <span>{{ $t('FieldsAndValidations.CloneQuestionnaireModel_NewTitle_Error_Required') }}</span>
                    </span>
                    <span v-if="$config.model.error" class="help-block field-validation-error">
                        <span>{{ $config.model.error }}</span>
                    </span>
                </div>
                <div class="form-group">
                    <label for="Comment" class="control-label">
                        {{ $t('Assignments.DetailsComments') }}
                    </label>
                    <textarea name="Comment" id="Comment" class="form-control"
                        :placeholder="$t('Assignments.EnterComments')" rows="6" maxlength="500"></textarea>
                </div>
                <input name="__RequestVerificationToken" type="hidden" :value="this.$hq.Util.getCsrfCookie()" />
                <input name="Id" type="hidden" :value="$config.model.id" />
                <input name="Version" type="hidden" :value="$config.model.version" />
                <input name="OriginalTitle" type="hidden" :value="$config.model.originalTitle" />
                <input name="IsCensus" type="hidden" :value="$config.model.isCensus" />

                <div class="action-buttons">
                    <button type="submit" class="btn btn-success">
                        {{ $t('QuestionnaireClonning.Clone') }}
                    </button>
                    <a href="../../SurveySetup" class="back-link">
                        {{ $t('Common.Cancel') }}
                    </a>
                </div>
            </form>
        </div>
    </HqLayout>
</template>
<script>

import Vue from 'vue'
import VeeValidate from 'vee-validate'
Vue.use(VeeValidate)

export default {
    methods: {
        async tryClone() {
            const validation = await this.$validator.validateAll()
            if (validation) {
                this.$refs.frmClone.submit()
            }
        },
    },
}
</script>
