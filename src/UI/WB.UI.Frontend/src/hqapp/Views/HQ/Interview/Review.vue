<template>
    <main class="web-interview web-interview-for-supervisor" :class="classes">
        <div class="container-fluid">
            <div class="row">
                <DetailsInfo />
                <Facets />
                <SearchResults />
                <Sidebar :showComplete="false" :show-foldback-button-as-hamburger="false" />
                <section class="questionnaire details-interview">
                    <Interview :interviewId="interviewId" mode="review" @connected="connected" />
                </section>
            </div>
        </div>
        <IdleTimeoutService />
        <portal-target name="body" multiple>
        </portal-target>
        <span id="loadingPixel" style="display:none" :data-loading="isLoading"></span>
    </main>
</template>

<script>
import Facets from './Facets'
import SearchResults from './SearchResults'
import Sidebar from '~/webinterview/components/Sidebar'
import DetailsInfo from './DetailsInfo.vue'
import { nextTick } from 'vue'
import http from '~/webinterview/api/http'
//const Interview = () => import('~/webinterview/components/Interview.vue')
//import Interview from '~/webinterview/components/Interview.vue'
import { defineAsyncComponent } from 'vue';

import '@/assets/css/markup-web-interview.scss'
import '@/assets/css/markup-interview-review.scss'

export default {
    watch: {
        ['$route.hash'](to) {
            if (to != null) {
                this.$store.dispatch('sectionRequireScroll', { id: to })
            }
        },
    },

    computed: {
        classes() {
            const sidebar = this.$store.state.webinterview.sidebar
            const smallOrMedioumScreenWidth = sidebar.screenWidth < sidebar.mediumScreenThreshold

            return {
                'show-content': smallOrMedioumScreenWidth && !sidebar.sidebarHidden,
                'show-filters': smallOrMedioumScreenWidth && !sidebar.facetHidden,
                'fullscreen-hidden-content': !smallOrMedioumScreenWidth && sidebar.sidebarHidden,
                'fullscreen-hidden-filters': !smallOrMedioumScreenWidth && sidebar.facetHidden,
                'filters-results-are-shown': !sidebar.searchResultsHidden,
            }
        },
        isLoading() {
            return this.$store.getters.loadingProgress === true ? 'true' : 'false'
        },
        interviewId() {
            return this.$route.params.interviewId
        },
    },

    methods: {
        onResize() {
            var screenWidth = document.documentElement.clientWidth
            this.$store.dispatch('screenWidthChanged', screenWidth)
        },

        connected() {
            this.$store.dispatch('getLanguageInfo')
            this.$store.dispatch('loadInterview')
            this.$store.dispatch('fetchFlags')
        },
    },

    beforeMount() {
        const app = this.$root;
        http.install(app, { store: this.$store });
    },

    mounted() {
        const self = this

        this.$nextTick(function () {
            window.addEventListener('resize', self.onResize)
            self.onResize()
        })
    },

    updated() {
        nextTick(() => {
            window.ajustNoticeHeight()
            window.ajustDetailsPanelHeight()
        })
    },

    components: {
        Facets,
        SearchResults,
        Sidebar,
        DetailsInfo,
        Interview: defineAsyncComponent(() => import('~/webinterview/components/Interview.vue')),
    },

    beforeDestroy() {
        window.removeEventListener('resize', this.onResize)
    },
}
</script>
