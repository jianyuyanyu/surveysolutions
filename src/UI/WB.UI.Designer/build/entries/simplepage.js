import { setupErrorHandler } from './errorsHandler';
import { installPageGuard } from '../../questionnaire/src/services/serverGuard';
setupErrorHandler();
installPageGuard();

import $ from 'jquery';
window.jQuery = window.$ = $;

import 'bootstrap';
import "../../Content/bootstrap-migrate.less"
import '/Scripts/custom/utils.js';

import './validation';
