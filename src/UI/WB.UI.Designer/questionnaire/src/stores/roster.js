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
        async questionDeleted(payload) {
            if (this.roster.itemId && this.questionnaireId) {
                if (!this.getIsDirty) {
                    await this.fetchRosterData(this.questionnaireId, this.roster.itemId);
                } else {
                    const freshData = await getRoster(this.questionnaireId, this.roster.itemId);
                    this.refreshContextualData(freshData);
                }
            }
        },
        async questionAdded(payload) {
            if (this.roster.itemId && this.questionnaireId) {
                if (!this.getIsDirty) {
                    await this.fetchRosterData(this.questionnaireId, this.roster.itemId);
                } else {
                    const freshData = await getRoster(this.questionnaireId, this.roster.itemId);
                    this.refreshContextualData(freshData);
                }
            }
        },
        async questionMoved(payload) {
            if (this.roster.itemId && this.questionnaireId) {
                if (!this.getIsDirty) {
                    await this.fetchRosterData(this.questionnaireId, this.roster.itemId);
                } else {
                    const freshData = await getRoster(this.questionnaireId, this.roster.itemId);
                    this.refreshContextualData(freshData);
                }
            }
        },
        async groupMoved(payload) {
            if (this.roster.itemId === payload.itemId && this.questionnaireId) {
                if (!this.getIsDirty) {
                    await this.fetchRosterData(this.questionnaireId, this.roster.itemId);
                } else {
                    // The roster was moved to a new parent while the user has unsaved edits.
                    // A full refresh would discard those edits, so only update the
                    // context-dependent question lists that are determined by location.
                    const freshData = await getRoster(this.questionnaireId, this.roster.itemId);
                    this.refreshContextualData(freshData);
                }
            }
        },
        // Updates only the fields that depend on the roster's structural position in the
        // questionnaire (available trigger question lists, breadcrumbs).  User edits to
        // title, type, variableName, fixedRosterTitles, etc. are intentionally preserved.
        // Both roster and initialRoster are patched so that dirty-detection continues to
        // reflect only genuine user changes, not stale server-side option lists.
        refreshContextualData(freshData) {
            const contextFields = [
                'numericIntegerQuestions',
                'textListsQuestions',
                'notLinkedMultiOptionQuestions',
                'numericIntegerTitles',
                'breadcrumbs'
            ];
            for (const field of contextFields) {
                if (freshData[field] !== undefined) {
                    this.roster[field] = _.cloneDeep(freshData[field]);
                    this.initialRoster[field] = _.cloneDeep(freshData[field]);
                }
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
