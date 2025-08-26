import { createRouter, createWebHistory, type Router, type RouteRecordRaw } from 'vue-router';
import AuditLog from '@/components/AuditLog.vue';
import OrderBook from '@/components/OrderBook.vue';

const routes: RouteRecordRaw[] = [
    { path: '/', component: OrderBook },
    { path: '/audit-log', component: AuditLog }
];

const router: Router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
    linkActiveClass: 'bg-green-700 text-white rounded-t-lg'
});

export default router;
