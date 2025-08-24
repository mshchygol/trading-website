import AuditLog from '@/components/AuditLog.vue'
import OrderBook from '@/components/OrderBook.vue'
import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        { path: '/', component: OrderBook },
        { path: '/audit-log', component: AuditLog },
    ],
    linkActiveClass: 'bg-green-700 text-white rounded-t-lg'
})

export default router
