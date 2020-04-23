<template>
    <div class="block-filter" 
        v-if="question != null && isSupported">
        <h5 :title="sanitizeHtml(question.questionText)">
            {{sanitizeHtml(question.questionText)}} <inline-selector :options="fieldOptions"
                no-empty
                v-if="fieldOptions != null"
                v-model="field" />
        </h5>

        <Typeahead
            v-if="question.type == 'SINGLEOPTION'"
            :control-id="'question-' + question.variable"           
            :placeholder="$t('Common.SelectOption')"            
            :values="options"
            :value="selectedOption"
            v-on:selected="optionSelected"/>
        
        <filter-input v-if="question.type == 'TEXT'"
            :value="condition.value"
            @input="input"                
            :id="'filter_' + condition.variable" />
        
        <filter-input v-if="question.type == 'NUMERIC'"
            :value="condition.value"
            type="number"
            @input="input"                
            :id="'filter_' + condition.variable" />
            
    </div>   
</template>
<script>

import gql from 'graphql-tag'
import { find, sortBy } from 'lodash'
import sanitizeHtml  from 'sanitize-html'

export default {
    props: {
        question: {type: Object },
        
        /** @type: {variable: string, value: string} */
        condition: { type: Object },
    },

    data() {
        return {
            field: null,
        }
    },

    methods: {
        getTypeaheadValues(options) {
            return options.map(o => {
                return {
                    key: o.value.toString(),
                    value: o.title,
                }
            })
        },

        input(value) {
            this.$emit('change', {
                variable: this.question.variable,
                field: this.field.id,
                value: value == null ? null : value.toLowerCase(),
            })
        },

        optionSelected(option) {
            this.$emit('change', {
                variable: this.question.variable,
                field: 'answerCode',
                value: option == null ? null : parseInt(option.key),
            })
        },

        sanitizeHtml: sanitizeHtml,
    },

    computed: { 
        options() {
            return this.getTypeaheadValues(sortBy(this.question.options, ['title']))
        },

        selectedOption() {
            let key = this.condition.value
            if(key != null) key = key.toString()
            return find(this.options, { key })
        },
        
        isSupported() {
            const supported = ['SINGLEOPTION', 'TEXT', 'NUMERIC']
            return find(supported, s => s == this.question.type)
        },

        fieldOptions() {
            switch(this.question.type) {
            case 'SINGLEOPTION': return null
            case 'TEXT': return [
                { id: 'answerLowerCase_starts_with', value: this.$t('Common.StartsWith') },
                { id: 'answerLowerCase', value: this.$t('Common.Equals') },
                        
            ]
            case 'NUMERIC': return [
                { id: 'answer', value: this.$t('Common.Equals')},
            ]}
            return null
        },
    },
}
</script>