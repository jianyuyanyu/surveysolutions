<template>
    <HqLayout :hasFilter="false" tag="workspaces-page">
        <template v-slot:headers>
            <div>
                <div class="topic-with-button">
                    <h1 v-dompurify-html="$t('MainMenu.Workspaces')"></h1>
                    <button type="button" v-if="$config.model.canManage" class="btn btn-success"
                        data-suso="create-new-workspace" @click="createNewWorkspace">
                        {{ $t('Workspaces.AddNew') }}
                    </button>
                </div>
                <i v-dompurify-html="$t('Workspaces.WorkspacesSubtitle')"></i>
                <div class="search-pusher"></div>
            </div>
        </template>
        <DataTables ref="table" data-suso="workspaces-list" :tableOptions="tableOptions" noSelect :noPaging="false"
            :contextMenuItems="contextMenuItems" :supportContextMenu="$config.model.canManage">
        </DataTables>

        <ModalFrame ref="createWorkspaceModal" :title="$t('Workspaces.CreateWorkspace')">
            <template v-slot:form>
                <Form @submit="createWorkspace" ref="createWorkspaceForm" data-suso="workspaces-create-dialog"
                    v-slot="{ errors }">
                    <div class="modal-body">
                        <div class="form-group" v-bind:class="{ 'has-error': errors.Name }">
                            <label class="control-label" for="newWorkspaceName">
                                {{ $t("Workspaces.Name") }}
                            </label>
                            <Field type="text" class="form-control" v-model.trim="newWorkspaceName" name="Name"
                                :label="$t('Workspaces.Name')" :rules="{
                                    required: true,
                                    max: 12,
                                    regex: /^[0-9,a-z]+$/,
                                    not_one_of: ['users', 'administration', 'api', 'apidocs', 'graphql', 'css', 'js', 'img', 'locale', 'fonts', '.vite', '@vite']
                                }" autocomplete="off" @keyup.enter="createWorkspace" id="newWorkspaceName" />

                            <p class="help-block" v-if="!errors.Name">
                                {{ $t('Workspaces.CanNotBeChanged') }}
                            </p>
                            <span v-else class="text-danger">
                                {{ errors.Name }}
                            </span>
                        </div>

                        <div class="form-group" v-bind:class="{ 'has-error': errors.DisplayName }">
                            <label class="control-label" for="newDescription">
                                {{ $t("Workspaces.DisplayName") }}
                            </label>
                            <Field type="text" class="form-control" v-model.trim="editedDisplayName" name="DisplayName"
                                :rules="{ required: true, max: 300 }" maxlength="300"
                                :label="$t('Workspaces.DisplayName')" autocomplete="off" @keyup.enter="createWorkspace"
                                id="newDescription" />
                            <p class="help-block" v-if="!errors.workspaceDisplayName">
                                {{ $t('Workspaces.DisplayNameHelpText') }}
                            </p>
                            <span v-else class="text-danger">{{ errors.DisplayName }}</span>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <div>
                            <button data-suso="workspace-create-save" v-bind:disabled="inProgress"
                                class="btn btn-primary" type="submit">
                                {{ $t("Common.Create") }}
                            </button>
                            <button type="button" class="btn btn-link" data-bs-dismiss="modal">
                                {{ $t("Common.Cancel") }}
                            </button>
                        </div>
                    </div>
                </Form>
            </template>
        </ModalFrame>

        <ModalFrame ref="editWorkspaceModal" data-suso="workspaces-edit-dialog"
            :title="$t('Workspaces.EditWorkspace', { name: editedRowId })">
            <template v-slot:form>
                <Form @submit="updateWorkspace" ref="editWorkspaceForm" v-slot="{ meta, errors }">
                    <div class="modal-body">
                        <div class="form-group" v-bind:class="{ 'has-error': meta.valid == false }">
                            <label class="control-label" for="editDescription">
                                {{ $t("Workspaces.DisplayName") }}
                            </label>
                            <Field type="text" class="form-control" v-model.trim="editedDisplayName" name="DisplayName"
                                :data-vv-as="$t('Workspaces.DisplayName')" :rules="{ required: true, max: 300 }"
                                maxlength="300" autocomplete="off" @keyup.enter="updateWorkspace"
                                id="editDescription" />
                            <span class="text-danger">{{ errors.DisplayName }}</span>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <div>
                            <button type="submit" data-suso="workspace-edit-save" class="btn btn-primary"
                                v-bind:disabled="inProgress">
                                {{ $t("Common.Save") }}
                            </button>
                            <button type="button" class="btn btn-link" data-suso="workspace-cancel"
                                data-bs-dismiss="modal">
                                {{ $t("Common.Cancel") }}
                            </button>
                        </div>
                    </div>
                </Form>
            </template>
        </ModalFrame>

        <ModalFrame ref="disableWorkspaceModal" data-suso="workspaces-disable-dialog"
            :title="$t('Workspaces.DisableWorkspacePopupTitle', { name: editedRowId })">
            <form onsubmit="return false;">
                <p>{{ $t("Workspaces.DisableExplanation") }}</p>
            </form>
            <template v-slot:actions>
                <div>
                    <button type="button" data-suso="workspace-disable-ok" class="btn btn-danger"
                        v-bind:disabled="inProgress" @click="disableWorkspace">
                        {{ $t("Common.Ok") }}
                    </button>
                    <button type="button" class="btn btn-link" data-suso="workspace-cancel" data-bs-dismiss="modal">
                        {{ $t("Common.Cancel") }}
                    </button>
                </div>
            </template>
        </ModalFrame>
        <DeleteWorkspaceModal ref="deleteWorkspaceModal" @workspace:deleted="loadData"></DeleteWorkspaceModal>
    </HqLayout>
</template>

<script>
import * as toastr from 'toastr'
import DeleteWorkspaceModal from './DeleteWorkspaceModal'
import moment from 'moment'
import { DateFormats } from '~/shared/helpers'
import { Form, Field, ErrorMessage } from 'vee-validate'
import { ref, nextTick } from 'vue'

export default {
    components: {
        Form,
        Field,
        ErrorMessage,
        DeleteWorkspaceModal,
    },

    data() {
        return {
            editedRowId: null,
            editedDisplayName: null,
            newWorkspaceName: null,
            inProgress: false,
            createWorkspaceForm: ref(),
            editWorkspaceForm: ref(),
        }
    },

    mounted() {
        this.loadData()
    },
    methods: {
        createNewWorkspace() {
            this.editedDisplayName = null
            this.newWorkspaceName = null

            this.$refs.createWorkspaceForm?.resetForm()
            this.$refs.createWorkspaceModal.modal('show')
        },
        loadData() {
            if (this.$refs.table) {
                this.$refs.table.reload()
            }
        },
        async updateWorkspace() {
            try {
                this.inProgress = true
                await this.$http.patch(`${this.$config.model.dataUrl}/${this.editedRowId}`, {
                    displayName: this.editedDisplayName,
                })
                this.$refs.editWorkspaceModal.hide()
                this.loadData()
            }
            finally {
                this.inProgress = false
            }
        },
        async disableWorkspace() {
            try {
                this.inProgress = true
                await this.$http.post(`${this.$config.model.dataUrl}/${this.editedRowId}/disable`)
                this.$refs.disableWorkspaceModal.hide()

                this.loadData()
            }
            catch (err) {
                const errors = err.response.data.Errors
                if (errors?.name) {
                    const nameErrors = errors.name.join('\r\n')
                    toastr.error(nameErrors)
                }
            }
            finally {
                this.inProgress = false
            }
        },
        async createWorkspace() {
            try {
                this.inProgress = true
                await this.$http.post(this.$config.model.dataUrl, {
                    displayName: this.editedDisplayName,
                    name: this.newWorkspaceName,
                })
                this.$refs.createWorkspaceModal.hide()
                this.loadData()
                this.editedDisplayName = null
                this.newWorkspaceName = null
            }
            catch (err) {
                let errorMessage = ''
                const errors = err.response.data.Errors
                if (errors) {
                    if ('Name' in errors) {
                        const nameErrors = errors.Name.join('\r\n')
                        errorMessage += this.$t('Workspaces.Name') + ': ' + nameErrors
                    }
                    if ('DisplayName' in errors) {
                        const displayNameErrors = errors.DisplayName.join('\r\n')
                        errorMessage += this.$t('Workspaces.DisplayName') + ': ' + displayNameErrors
                    }
                }
                if (errorMessage) {
                    toastr.error(errorMessage)
                }
            }
            finally {
                this.inProgress = false
            }
        },
        contextMenuItems({ rowData }) {
            let items = []

            items.push({
                name: this.$t('Workspaces.Open'),
                className: 'suso-open',
                callback: (_, opt) => {
                    window.location = this.workspacePath(rowData.Name)
                },
            })

            if (!this.$config.model.canManage)
                return items

            if (rowData.isDisabled) {
                items.push({
                    name: this.$t('Workspaces.Enable'),
                    className: 'suso-enable',
                    callback: (_, opt) => {
                        this.$http.post(`${this.$config.model.dataUrl}/${rowData.Name}/enable`)
                            .then(() => {
                                this.loadData()
                            })
                    },
                },
                    {
                        name: this.$t('Common.Delete'),
                        className: 'suso-delete',
                        callback: (_, opt) => {
                            const parsedRowId = rowData.Name
                            this.editedRowId = parsedRowId

                            this.$refs.deleteWorkspaceModal.showModal(rowData.Name)
                        },
                    })
            }
            else {
                items.push(
                    {
                        name: this.$t('Workspaces.Edit'),
                        className: 'suso-edit',
                        callback: (_, opt) => {
                            const parsedRowId = rowData.Name
                            this.editedRowId = parsedRowId
                            this.editedDisplayName = rowData.DisplayName

                            this.$refs.editWorkspaceModal.modal('show')
                            nextTick(() => {
                                this.$refs.editWorkspaceForm.resetForm()
                            })
                        },
                    },
                    {
                        name: this.$t('Workspaces.WorkspaceSettings'),
                        className: 'suso-settings',
                        callback: (_, opt) => {
                            window.location = this.workspacePath(rowData.Name) + 'Settings'
                        },
                    },
                    {
                        name: this.$t('Common.EmailProviders'),
                        className: 'suso-email',
                        callback: (_, opt) => {
                            window.location = this.workspacePath(rowData.Name) + 'Settings/EmailProviders'
                        },
                    },
                    {
                        name: this.$t('TabletLogs.PageTitle'),
                        className: 'suso-logs',
                        callback: (_, opt) => {
                            window.location = this.workspacePath(rowData.Name) + 'Diagnostics/Logs'
                        },
                    },
                    {
                        name: this.$t('Common.AuditLog'),
                        className: 'suso-audit',
                        callback: (_, opt) => {
                            window.location = this.workspacePath(rowData.Name) + 'Diagnostics/AuditLog'
                        },
                    },
                    {
                        name: this.$t('Pages.InterviewPackages'),
                        className: 'suso-interview-packages',
                        callback: (_, opt) => {
                            window.location = this.workspacePath(rowData.Name) + 'Diagnostics/InterviewPackages'
                        },
                    }
                )

                if (rowData.Name != 'primary') {
                    items.push({
                        name: this.$t('Workspaces.Disable'),
                        className: 'suso-disable',
                        callback: (_, opt) => {
                            const parsedRowId = rowData.Name
                            this.editedRowId = parsedRowId

                            this.$refs.disableWorkspaceModal.modal('show')
                        },
                    })

                    items.push({
                        name: this.$t('Common.Delete'),
                        className: 'suso-delete',
                        callback: (_, opt) => {
                            const parsedRowId = rowData.Name
                            this.editedRowId = parsedRowId

                            this.$refs.deleteWorkspaceModal.showModal(rowData.Name)
                        },
                    })
                }
            }

            return items
        },

        workspacePath(workspace) {
            return this.$hq.basePath.replace(this.$config.workspace, workspace)
        },

    },
    computed: {
        model() {
            return this.$config.model
        },

        tableOptions() {
            var self = this
            return {
                deferLoading: 0,
                columns: [
                    {
                        data: 'Name',
                        name: 'Name',
                        title: this.$t('Workspaces.Name'),
                        sortable: true,
                        render(data, type, row) {
                            const workspaceUrl = self.workspacePath(data)
                            return `<a href='${workspaceUrl}'>${data}</a>`
                        },
                    },
                    {
                        data: 'DisplayName',
                        name: 'DisplayName',
                        title: this.$t('Workspaces.DisplayName'),
                        sortable: true,
                        render(data, type, row) {
                            return $('<div>').text(data).html()
                        },
                    },
                    {
                        data: 'CreatedAtUtc',
                        name: 'CreatedAtUtc',
                        title: this.$t('Workspaces.CreatedAt'),
                        tooltip: this.$t('Workspaces.Tooltip_Table_CreatedAt'),
                        sortable: true,
                        searchable: false,
                        render(data) {
                            if (data)
                                return moment.utc(data).local().format(DateFormats.dateTime)
                            return ''
                        },
                    },
                ],
                rowId: function (row) {
                    return row.name
                },
                ajax: {
                    url: `${this.$config.model.dataUrl}?IncludeDisabled=true`,
                    type: 'GET',
                    dataSrc: function (responseJson) {
                        responseJson.recordsTotal = responseJson.TotalCount
                        responseJson.recordsFiltered = responseJson.TotalCount
                        responseJson.Workspaces.forEach(w => {
                            w.isDisabled = w.DisabledAtUtc != null
                        })

                        return responseJson.Workspaces
                    },
                    contentType: 'application/json',
                },
                responsive: false,
                order: [[1, 'asc']],
                sDom: 'rf<"table-with-scroll"t>ip',
            }
        },
    },
}
</script>
