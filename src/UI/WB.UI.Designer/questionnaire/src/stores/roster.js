import { defineStore } from 'pinia';
import { getRoster } from '../services/rosterService';
import { get, commandCall } from '../services/apiService';
import emitter from '../services/emitter';
import _ from 'lodash';

export const useRosterStore = defineStore('roster', {
    state: () => ({
        roster: {},
        initialRoster: {},
        questionnaireId: null
    }),
    getters: {
        getRoster: state => state.roster,
        getInitialRoster: state => state.initialRoster,
        getIsDirty: state => !_.isEqual(state.roster, state.initialRoster)
    },
    actions: {
        setupListeners() {
            emitter.on('rosterUpdated', this.rosterUpdated);
            emitter.on('rosterDeleted', this.rosterDeleted);
            emitter.on('questionDeleted', this.questionDeleted);
            emitter.on('questionAdded', this.questionAdded);
            emitter.on('questionMoved', this.questionMoved);
            emitter.on('groupMoved', this.groupMoved);
        },
        rosterUpdated(payload) {
            if (this.roster.itemId === payload.roster.itemId) {
                this.setRosterData(payload.roster);
            }
        },
        rosterDeleted(payload) {
            if (this.roster.itemId === payload.id) {
                this.clear();
            }
        },
        questionDeleted(payload) {
            if (this.roster.itemId && this.questionnaireId) {
                this.fetchRosterData(this.questionnaireId, this.roster.itemId);
            }
        },
        async questionAdded(payload) {
            if (this.roster.itemId && this.questionnaireId) {
                await this.fetchRosterData(this.questionnaireId, this.roster.itemId);
            }
        },
        async questionMoved(payload) {
            if (this.roster.itemId && this.questionnaireId) {
                await this.fetchRosterData(this.questionnaireId, this.roster.itemId);
            }
        },
        async groupMoved(payload) {
            if (this.roster.itemId === payload.itemId && this.questionnaireId) {
                await this.fetchRosterData(this.questionnaireId, this.roster.itemId);
            }
        },
        async fetchRosterData(questionnaireId, rosterId) {
            this.questionnaireId = questionnaireId;
            const data = await getRoster(questionnaireId, rosterId);
            this.setRosterData(data);
        },

        setRosterData(data) {
            this.initialRoster = _.cloneDeep(data);
            this.roster = _.cloneDeep(this.initialRoster);
        },

        clear() {
            this.roster = {};
            this.initialRoster = {};
            this.questionnaireId = null;
        },

        discardChanges() {
            this.roster = _.cloneDeep(this.initialRoster);
        }
    }
});
