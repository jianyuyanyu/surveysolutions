import '../assets/css/markup.scss'

import axios from 'axios'
import { installAxiosInterceptors } from '~/shared/serverValidator'

// No API calls are made on this page, so we explicitly validate
// the X-Survey-Solutions response header via an interceptor-equipped axios instance.
installAxiosInterceptors(axios)
axios.get('/.version').catch(() => {})

