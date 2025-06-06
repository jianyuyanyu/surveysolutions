<template>
    <HqLayout :hasFilter="false">
        <template v-slot:headers>
            <div>
                <ol class="breadcrumb">
                    <li>
                        <a v-bind:href="referrerUrl">{{ referrerTitle }}</a>
                    </li>
                </ol>
                <h1>{{ title }}</h1>
            </div>
        </template>
        <div class="extra-margin-bottom">
            <div class="profile">
                <div class="col-sm-7">
                    <p>
                        <span v-dompurify-html="$t('Pages.User_CreateText1')"></span>
                        <br />
                        <span v-dompurify-html="$t('Pages.User_CreateText2', { link: uploadUri })"></span>
                    </p>
                </div>
                <div class="col-sm-12">
                    <form-group :label="$t('Pages.UsersManage_WorkspacesFilterPlaceholder')"
                        :error="modelState['Workspace']" :mandatory="true">
                        <div class="field form-control" :class="{ answered: workspace != null }"
                            style="padding:0 10px 0 0">
                            <Typeahead control-id="workspace" :value="workspace" :ajax-params="{}"
                                :fetch-url="model.api.workspacesUrl" @selected="workspaceSelected"></Typeahead>
                        </div>
                    </form-group>
                    <form-group :label="$t('Pages.UsersManage_RoleFilterPlaceholder')" :error="modelState['Role']"
                        :mandatory="true">
                        <div class="field form-control" :class="{ answered: role != null }" style="padding:0 10px 0 0">
                            <Typeahead control-id="role" :value="role" :values="model.roles" @selected="roleSelected">
                            </Typeahead>
                        </div>
                    </form-group>
                    <form-group :label="$t('FieldsAndValidations.UserNameFieldName')" :error="modelState['UserName']"
                        :mandatory="true">
                        <TextInput v-model.trim="userName" :haserror="modelState['UserName'] !== undefined"
                            id="UserName" />
                    </form-group>
                    <form-group v-if="isInterviewer" :label="$t('Pages.Interviewers_SupervisorTitle')"
                        :error="modelState['SupervisorId']" :mandatory="true">
                        <div class="field" :class="{ answered: supervisor != null }">
                            <Typeahead control-id="supervisor" :value="supervisor"
                                :ajax-params="{ workspace: (this.workspace || {}).key }"
                                :fetch-url="$config.model.api.supervisorWorkspaceUrl" @selected="supervisorSelected">
                            </Typeahead>
                        </div>
                    </form-group>
                    <form-group :label="$t('FieldsAndValidations.NewPasswordFieldName')" :error="modelState['Password']"
                        :mandatory="true">
                        <TextInput type="password" v-model.trim="password"
                            :haserror="modelState['Password'] !== undefined" id="Password" />
                    </form-group>
                    <form-group :label="$t('FieldsAndValidations.ConfirmPasswordFieldName')"
                        :error="modelState['ConfirmPassword']" :mandatory="true">
                        <TextInput type="password" v-model.trim="confirmPassword"
                            :haserror="modelState['ConfirmPassword'] !== undefined" id="ConfirmPassword" />
                    </form-group>
                    <div class="block-filter">
                        <input id="ShowPassword" type="checkbox" style="margin-right:5px"
                            onclick="var pass = document.getElementById('Password');pass.type = (pass.type === 'text' ? 'password' : 'text');var confirm = document.getElementById('ConfirmPassword');confirm.type = (confirm.type === 'text' ? 'password' : 'text');">
                        <label for="ShowPassword">
                            <span></span>{{ $t('Pages.ShowPassword') }}
                        </label>
                    </div>
                </div>
                <div class="col-sm-12">
                    <div class="separate-line"></div>
                </div>
                <div class="col-sm-12">
                    <h5 class="extra-margin-bottom" v-dompurify-html="$t('Pages.PublicSection')"></h5>
                    <form-group :label="$t('FieldsAndValidations.PersonNameFieldName')"
                        :error="modelState['PersonName']">
                        <TextInput v-model.trim="personName" :haserror="modelState['PersonName'] !== undefined"
                            id="PersonName" />
                    </form-group>
                    <form-group :label="$t('FieldsAndValidations.EmailFieldName')" :error="modelState['Email']">
                        <TextInput v-model.trim="email" :haserror="modelState['Email'] !== undefined" id="Email" />
                    </form-group>
                    <form-group :label="$t('FieldsAndValidations.PhoneNumberFieldName')"
                        :error="modelState['PhoneNumber']">
                        <TextInput v-model.trim="phoneNumber" :haserror="modelState['PhoneNumber'] !== undefined"
                            id="PhoneNumber" />
                    </form-group>
                </div>

                <div class="col-sm-12">
                    <div class="block-filter">
                        <button type="submit" class="btn btn-success" style="margin-right:5px" id="btnCreate"
                            @click="createAccount">{{ $t('Pages.Create') }}</button>
                        <a class="btn btn-default" v-bind:href="referrerUrl" id="lnkCancel">
                            {{ $t('Common.Cancel') }}
                        </a>
                    </div>
                </div>
            </div>
        </div>
    </HqLayout>
</template>

<script>
import { each } from 'lodash'
import { RoleNames } from '~/shared/constants'

export default {
    title: (context) => context.title,
    data() {
        return {
            modelState: {},
            userName: null,
            personName: null,
            email: null,
            phoneNumber: null,
            oldPassword: null,
            password: null,
            confirmPassword: null,
            isLockedByHeadquarters: false,
            isLockedBySupervisor: false,
            successMessage: null,
            supervisor: null,
            workspace: null,
            role: null,
        }
    },
    computed: {
        model() {
            return this.$config.model
        },
        uploadUri() {
            const url = this.$hq.basePath + 'Upload'
            const link = '<a href="' + url + '" target="_blank">' + this.$t('Pages.User_CreateText_UserBatchUploadLinkText') + '</a>'
            return link
        },
        userInfo() {
            return this.model.userInfo
        },
        isHeadquarters() {
            return this.role && this.role.key == RoleNames.HQ
        },
        isSupervisor() {
            return this.role && this.role.key == RoleNames.SUPERVISOR
        },
        isInterviewer() {
            return this.role && this.role.key == RoleNames.INTERVIEWER
        },
        isObserver() {
            return this.role && this.role.key == RoleNames.OBSERVER
        },
        isApiUser() {
            return this.role && this.role.key == RoleNames.API
        },
        referrerTitle() {
            if (this.isHeadquarters) return this.$t('Pages.Profile_HeadquartersList')
            if (this.isSupervisor) return this.$t('Pages.Profile_SupervisorsList')
            if (this.isInterviewer) return this.$t('Pages.Profile_InterviewersList')
            if (this.isObserver) return this.$t('Pages.Profile_ObserversList')
            if (this.isApiUser) return this.$t('Pages.Profile_ApiUsersList')

            return this.$t('Pages.Home')
        },
        referrerUrl() {
            return '/users/UsersManagement'
        },
        title() {
            if (this.role)
                return `${this.$t('Pages.Create')} ${this.$t(`Roles.${this.role.key}`)}`
            return this.$t('Pages.Create')
        },
    },
    watch: {
        userName: function (val) {
            delete this.modelState['UserName']
        },
        personName: function (val) {
            delete this.modelState['PersonName']
        },
        email: function (val) {
            delete this.modelState['Email']
        },
        phoneNumber: function (val) {
            delete this.modelState['PhoneNumber']
        },
        password: function (val) {
            delete this.modelState['Password']
        },
        confirmPassword: function (val) {
            delete this.modelState['ConfirmPassword']
        },
        supervisor: function (val) {
            delete this.modelState['SupervisorId']
        },
        workspace: function (val) {
            delete this.modelState['Workspace']
        },
        role: function (val) {
            delete this.modelState['Role']
        },
        title: function (val) {
            this.$title = val
        },
    },
    methods: {
        supervisorSelected(newValue) {
            this.supervisor = newValue
        },
        workspaceSelected(newValue) {
            this.workspace = newValue
        },
        roleSelected(newValue) {
            this.role = newValue
        },
        createAccount: function (event) {
            this.successMessage = null
            for (var error in this.modelState) {
                delete this.modelState[error]
            }

            var self = this
            this.$http({
                method: 'post',
                url: this.model.api.createUserUrl,
                data: {
                    supervisorId: (self.supervisor || {}).key,
                    userName: self.userName,
                    personName: self.personName,
                    email: self.email,
                    phoneNumber: self.phoneNumber,
                    isLockedByHeadquarters: self.isLockedByHeadquarters,
                    isLockedBySupervisor: self.isLockedBySupervisor,
                    password: self.password,
                    confirmPassword: self.confirmPassword,
                    role: (self.role || {}).key,
                    workspace: (self.workspace || {}).key,
                },
                headers: {
                    'X-CSRF-TOKEN': this.$hq.Util.getCsrfCookie(),
                },
            }).then(
                response => {
                    window.location.href = self.referrerUrl
                },
                error => {
                    self.processModelState(error.response.data, self)
                }
            )
        },
        processModelState: function (response, vm) {
            if (response) {
                each(response, function (state) {
                    var message = ''
                    var stateErrors = state.value
                    if (stateErrors) {
                        each(stateErrors, function (stateError, j) {
                            if (j > 0) {
                                message += '; '
                            }
                            message += stateError
                        })
                        vm.modelState[state.key] = message
                    }
                })
            }
        },
    },
}
</script>
