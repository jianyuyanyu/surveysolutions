<template>
    <div class="row">
        <div class="col-md-5">
            <div class="checkbox checkbox-in-column">
                <input id="cb-is-integer" type="checkbox" class="wb-checkbox" v-model="activeQuestion.isInteger"
                    @change="isIntegerChange($event)" />
                <label for="cb-is-integer"><span></span>{{ $t('QuestionnaireEditor.QuestionInteger') }}</label>
            </div>
        </div>
        <div class="col-md-5 inline-inputs">
            <div class="form-group checkbox-in-column" v-show="!activeQuestion.isInteger"
                :class="{ 'has-error': !isValidCountOfDecimalPlaces }">
                <label for="edit-question-count-decimal">{{ $t('QuestionnaireEditor.QuestionDecimalPlaces') }}</label>
                <input id="edit-question-count-decimal" type="number" inputmode="numeric" maxlength="9" min="1"
                    max="2147483647" v-number="/^(\d*)$/" name="countOfDecimalPlaces"
                    v-model.number="activeQuestion.countOfDecimalPlaces" class="form-control small-numeric-input">
                <p class="help-block ng-cloak" v-show="!isValidCountOfDecimalPlaces">
                    {{ $t('QuestionnaireEditor.QuestionOnlyInts') }}
                </p>
            </div>
        </div>

    </div>
    <div class="row">
        <div class="col-md-6 inline-inputs">
            <div class="checkbox checkbox-in-column">
                <input id="cb-use-formatting" type="checkbox" class="wb-checkbox"
                    v-model="activeQuestion.useFormatting" />
                <label for="cb-use-formatting"><span></span>{{ $t('QuestionnaireEditor.QuestionUseSeparator') }}</label>
                <help link="useFormatting"></help>
            </div>
        </div>
    </div>
    <div class="row">
        <!--div class="col-md-12" ng-include="'views/question-details/OptionsEditor-template.html'"></div-->
        <OptionsEditorTemplate ref="options" :activeQuestion="activeQuestion" :questionnaireId="questionnaireId">
        </OptionsEditorTemplate>
    </div>
</template>

<script>
import Help from './../Help.vue'
import { isInteger } from '../../../../helpers/number';
import OptionsEditorTemplate from './OptionsEditorTemplate.vue'

export default {
    name: 'NumericQuestion',
    expose: ['prepareToSave'],
    components: {
        Help,
        OptionsEditorTemplate,
    },
    props: {
        activeQuestion: { type: Object, required: true }
    },
    data() {
        return {}
    },
    computed: {
        isValidCountOfDecimalPlaces() {
            if (!this.activeQuestion.countOfDecimalPlaces)
                return true;
            return isInteger(this.activeQuestion.countOfDecimalPlaces);
        },
    },
    methods: {
        async prepareToSave() {
            await this.$refs.options.showOptionsInList();
        },
        isIntegerChange(event) {
            //this.activeQuestion.isInteger = event.target.checked;
        },
    }
}
</script>
