import Vue from "vue"
import VueRouter from "vue-router"

Vue.use(VueRouter)

import Complete from "../components/Complete"
import Cover from "../components/Cover"
import Section from "../components/Section"
import SideBar from "../components/Sidebar"

function NewRouter(store) {

    const router = new VueRouter({
        base: Vue.$config.virtualPath + "/",
        mode: "history",
        routes: [
            {
                name: "prefilled",
                path: "/:interviewId/Cover",
                components: {
                    default: Cover,
                    sideBar: SideBar
                }
            },
            {
                name: "section",
                path: "/:interviewId/Section/:sectionId",
                components: {
                    default: Section,
                    sideBar: SideBar
                }
            },
            {
                name: "complete",
                path: "/:interviewId/Complete",
                components: {
                    default: Complete,
                    sideBar: SideBar
                }
            },
            {
                name: "finish",
                path: "/Finish/:interviewId"
            }
        ],
        scrollBehavior(to, from, savedPosition) {
            if (savedPosition) {
                store.dispatch("sectionRequireScroll", { id: (from.params).sectionId })
            } else {
                return { x: 0, y: 0 }
            }
        }
    })

    // tslint:disable:no-string-literal
    router.beforeEach(async (to, from, next) => {
        await Vue.$api.hub({ interviewId: to.params["interviewId"] })

        if(to.params.sectionId == null)
            await store.dispatch("changeSection", null)
        next();
    })

    router.afterEach(() => {
        const hamburger = document.getElementById("sidebarHamburger")

        // check for button visibility.
        if (hamburger && hamburger.offsetParent != null) {
            store.dispatch("toggleSidebarPanel", false /* close sidebar panel */)
        }
    })

    store.subscribeAction((action, state) => {
        switch (action.type) {
            case "finishInterview":
                location.replace(router.resolve({ name: "finish", params: { interviewId: state.route.params.interviewId } }).href)
                break;
            case "navigateTo":
                router.replace(action.payload)
                break;
        }
    })

    return router;
}

export default NewRouter;