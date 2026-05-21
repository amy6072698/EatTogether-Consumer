import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import apiFetch from '@/utils/apiFetch.js'

const INITIAL_MEMBER = () => ({
    id: null,
    account: '',
    name: '',
    email: '',
    birthDate: null,
    avatarFileName: null,
    googleAvatarUrl: null,
    googleEmail: null,
    hashedPasswordStatus: '', // 'HAS_PASSWORD' | 'EXTERNAL_LOGIN_NO_PASSWORD'
    googleLinked: false,
})

export const useAuthStore = defineStore('auth', () => {
    // ── State ──────────────────────────────────────────────
    const member = ref(INITIAL_MEMBER())
    const isLoggedIn = ref(false)
    const isLoading = ref(false)

    // ── Getters ────────────────────────────────────────────
    /** 取 member.name 第一個字元，未登入或 name 為空時回傳空字串 */
    const avatarInitial = computed(() => member.value.name?.[0] ?? '')

    // ── Actions ────────────────────────────────────────────

    function setAuth(data) {
        member.value = { ...INITIAL_MEMBER(), ...data }
        isLoggedIn.value = true
    }

    /**
     * 清空登入狀態（供 apiFetch refresh 失敗時呼叫）
     * 不呼叫後端 API，純粹清空前端 store 狀態
     */
    function clearAuth() {
        member.value = INITIAL_MEMBER()
        isLoggedIn.value = false
    }

    /**
     * 取得登入會員資料
     * 路由守衛（0-F8）會等待此函式完成再判斷 isLoggedIn
     * apiFetch 若遇網路錯誤會 throw，此處 catch 後靜默清空，不讓錯誤繼續往上傳
     */
    async function fetchMe() {
        isLoading.value = true
        try {
            const res = await apiFetch('/members/me')

            if (res.ok) {
                member.value = await res.json()
                isLoggedIn.value = true
            } else if (res.status === 401) {
                // apiFetch 已嘗試 refresh，仍失敗 → 靜默清空，不拋錯
                clearAuth()
            } else {
                // 其他非 ok 狀態（404、500 等）→ 靜默清空
                clearAuth()
            }
        } catch {
            // apiFetch throw 代表網路層錯誤（後端未啟動、連線中斷等）
            // Toast 已由 apiFetch 顯示，此處靜默清空，不讓錯誤繼續往上傳
            clearAuth()
        } finally {
            isLoading.value = false
        }
    }

    /**
     * 登出：清除後端 Cookie 並清空前端狀態
     * router 使用動態 import 避免循環依賴（auth store → router → auth store）
     */
    async function logout() {
        await apiFetch('/auth/logout', { method: 'POST' })

        // 無論後端成功或失敗，一律清空前端狀態
        clearAuth()

        const { default: router } = await import('@/router/index.js')
        router.push('/')
    }

    /**
     * 初始化時確認登入狀態
     * 已確認過（isLoggedIn === true）就直接 return，避免重複呼叫 API
     */
    async function checkAuth() {
        if (isLoggedIn.value) return
        await fetchMe()
    }

    return {
        member,
        isLoggedIn,
        isLoading,
        avatarInitial,
        setAuth,
        clearAuth,
        fetchMe,
        logout,
        checkAuth,
    }
})
