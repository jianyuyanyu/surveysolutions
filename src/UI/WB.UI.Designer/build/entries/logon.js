import { setupErrorHandler } from './errorsHandler';
import { installPageGuard } from '../../questionnaire/src/services/serverGuard';
setupErrorHandler();
installPageGuard();

import $ from 'jquery';
window.jQuery = window.$ = $;

import '../../Content/bootstrap-migrate.less';
import '/Scripts/custom/utils.js';

import './validation';

import '/questionnaire/content/designer-start/designer-start.less';
