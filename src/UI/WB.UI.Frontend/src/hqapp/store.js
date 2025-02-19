import { createStore } from 'vuex'
import routeParams from '../shared/stores/store.routeParams'

export function registerStore(vue) {

    const store = createStore({
        modules: { route: routeParams },
        getters: {
            workspace() {
                return window.CONFIG.workspace
            },
        },
    })

    vue.use(store)

    return store
}
