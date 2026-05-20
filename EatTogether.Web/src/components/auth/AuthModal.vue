<script setup>
import { onMounted, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { Modal } from 'bootstrap'
import apiFetch from '@/utils/apiFetch.js'
import Button from '@/components/common/Button.vue'
import { useAuthStore } from '@/stores/auth.js'
import { generateState, buildGoogleOAuthUrl } from '@/utils/googleOAuth.js'
import FormErrorMessage from '@/components/common/FormErrorMessage.vue'

const router = useRouter()
const route = useRoute()

const authStore = useAuthStore()

// Tab 狀態：'login' | 'register'
const activeTab = ref('login')

// ── 登入 ──
const loginAccount = ref('')
const loginPassword = ref('')
const loginFormError = ref('')
const loginSuccess = ref(false)
const isLoginSubmitting = ref(false)
const showResendEmail = ref(false)
const loginResendEmail = ref('')
const isResendSubmitting = ref(false)
const showAccountDeleted = ref(false) // 控制帳號已刪除提示區塊
const isRestoring = ref(false) // 重新啟用按鈕 loading 狀態

// 註冊成功狀態
const registerSuccess = ref(false)

// 註冊表單欄位
const regAccount = ref('')
const regName = ref('')
const regEmail = ref('')
const regPassword = ref('')
const regConfirmPassword = ref('')

// 欄位錯誤訊息
const regAccountError = ref('')
const regNameError = ref('')
const regEmailError = ref('')
const regPasswordError = ref('')
const regConfirmPasswordError = ref('')
const regFormError = ref('')

const isSubmitting = ref(false)

// 密碼顯示切換
const showLoginPassword = ref(false)
const showPassword = ref(false)
const showConfirmPassword = ref(false)

function validateAccount() {
    regAccountError.value = ''
    if (!regAccount.value.trim()) {
        regAccountError.value = '帳號為必填'
    } else if (!/^[a-zA-Z0-9_]{3,50}$/.test(regAccount.value)) {
        regAccountError.value = '帳號限英數字及底線，3–50 字元'
    }
}

function validateName() {
    regNameError.value = ''
    const trimmed = regName.value.trim()
    if (!trimmed) {
        regNameError.value = '姓名為必填'
    } else if (trimmed.length > 50) {
        regNameError.value = '姓名最多 50 字'
    }
}

function validateEmail() {
    regEmailError.value = ''
    const trimmed = regEmail.value.trim()
    if (!trimmed) {
        regEmailError.value = 'Email 為必填'
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed)) {
        regEmailError.value = 'Email 格式不正確'
    } else if (trimmed.length > 100) {
        regEmailError.value = 'Email 最長 100 字元'
    }
}

function validatePassword() {
    regPasswordError.value = ''
    if (!regPassword.value) {
        regPasswordError.value = '密碼為必填'
    } else if (regPassword.value.length < 8) {
        regPasswordError.value = '密碼至少需要 8 個字元'
    } else if (regPassword.value.length > 128) {
        regPasswordError.value = '密碼最多 128 個字元'
    }
}

function validateConfirmPassword() {
    regConfirmPasswordError.value = ''
    if (!regConfirmPassword.value) {
        regConfirmPasswordError.value = '請再次輸入密碼'
    } else if (regConfirmPassword.value !== regPassword.value) {
        regConfirmPasswordError.value = '兩次密碼輸入不一致'
    }
}

async function handleLogin() {
    loginFormError.value = ''
    showResendEmail.value = false
    loginResendEmail.value = ''
    isLoginSubmitting.value = true
    try {
        const res = await apiFetch('/auth/login', {
            method: 'POST',
            body: JSON.stringify({
                account: loginAccount.value,
                password: loginPassword.value,
            }),
        })

        if (res.ok) {
            await authStore.fetchMe()
            const modalEl = document.querySelector('#authModal')
            Modal.getInstance(modalEl)?.hide()
            const redirect = route.query.redirect
            if (redirect) router.push(redirect)
            return
        }

        if (res.status >= 500) return
        if (res.status === 429) {
            loginFormError.value = '請求過於頻繁，請稍後再試'
            return
        }
        if (res.status === 403) {
            loginFormError.value = '帳號已停權，請聯繫餐廳'
            return
        }

        const data = await res.json()

        if (data.errorCode === 'email_not_verified') {
            loginFormError.value = '請先驗證 Email'
            showResendEmail.value = true
            return
        }
        if (data.errorCode === 'account_blacklisted') {
            loginFormError.value = '帳號已停權，請聯繫餐廳'
            return
        }
        if (data.errorCode === 'account_deleted') {
            showAccountDeleted.value = true
            return
        }
        loginFormError.value = data.message || '帳號或密碼錯誤'
    } catch (err) {
        if (import.meta.env.DEV) {
            console.error('[handleLogin]', err)
        }
    } finally {
        isLoginSubmitting.value = false
    }
}

async function handleResendVerifyEmail() {
    if (!loginResendEmail.value.trim()) return
    isResendSubmitting.value = true
    try {
        await apiFetch('/auth/resend-verify-email', {
            method: 'POST',
            body: JSON.stringify({ email: loginResendEmail.value.trim() }),
        })
    } catch (err) {
        if (import.meta.env.DEV) {
            console.error('[handleResendVerifyEmail]', err)
        }
    } finally {
        isResendSubmitting.value = false
    }
}

async function handleRestoreAccount() {
    isRestoring.value = true
    try {
        const res = await apiFetch('/auth/restore-account', {
            method: 'POST',
            body: JSON.stringify({
                account: loginAccount.value,
                password: loginPassword.value,
            }),
        })

        if (res.ok) {
            await authStore.fetchMe()
            showAccountDeleted.value = false
            const modalEl = document.querySelector('#authModal')
            Modal.getInstance(modalEl)?.hide()
            const redirect = route.query.redirect
            if (redirect) router.push(redirect)
            return
        }

        if (res.status >= 500) return
        if (res.status === 429) {
            loginFormError.value = '請求過於頻繁，請稍後再試'
            return
        }
        if (res.status === 403) {
            showAccountDeleted.value = false
            loginFormError.value = '帳號已停權，請聯繫客服'
            return
        }

        const data = await res.json()
        loginFormError.value = data?.message || '復原失敗，請稍後再試'
        showAccountDeleted.value = false
    } catch (err) {
        if (import.meta.env.DEV) {
            console.error('[handleRestoreAccount]', err)
        }
    } finally {
        isRestoring.value = false
    }
}

function handleGoogleLogin() {
    const state = generateState(route.fullPath)
    const url = buildGoogleOAuthUrl(state)
    window.location.href = url
}

function handleForgotPassword() {
    const authModalEl = document.querySelector('#authModal')
    const fpModalEl = document.querySelector('#forgotPasswordModal')
    Modal.getInstance(authModalEl)?.hide()
    // 等 authModal 完全關閉後才開啟 forgotPasswordModal，避免兩個 Modal 同時存在
    authModalEl.addEventListener(
        'hidden.bs.modal',
        () => {
            Modal.getOrCreateInstance(fpModalEl).show()
        },
        { once: true }
    )
}

function validateRegisterForm() {
    validateAccount()
    validateName()
    validateEmail()
    validatePassword()
    validateConfirmPassword()

    return !regAccountError.value && !regNameError.value && !regEmailError.value && !regPasswordError.value && !regConfirmPasswordError.value
}

async function handleRegister() {
    if (!validateRegisterForm()) return

    regFormError.value = ''
    isSubmitting.value = true
    try {
        const res = await apiFetch('/auth/register', {
            method: 'POST',
            body: JSON.stringify({
                account: regAccount.value,
                name: regName.value.trim(),
                email: regEmail.value.trim(),
                password: regPassword.value,
            }),
        })

        if (res.ok) {
            registerSuccess.value = true
            return
        }

        if (res.status >= 500) return
        if (res.status === 429) {
            regFormError.value = '請求過於頻繁，請稍後再試'
            return
        }

        const data = await res.json()

        if (res.status === 409) {
            if (data.errorCode === 'account_taken') {
                regAccountError.value = '此帳號已被使用，請換一個'
            } else if (data.errorCode === 'email_taken') {
                regEmailError.value = '此 Email 已有帳號，請直接登入'
            } else {
                regFormError.value = data.message || '發生錯誤，請稍後再試'
            }
            return
        }

        regFormError.value = data?.message || '發生錯誤，請稍後再試'
    } catch (err) {
        if (import.meta.env.DEV) {
            console.error('[handleRegister]', err)
        }
    } finally {
        isSubmitting.value = false
    }
}

function switchToRegister() {
    activeTab.value = 'register'
    loginAccount.value = ''
    loginPassword.value = ''
    loginFormError.value = ''
    loginSuccess.value = false
    showResendEmail.value = false
    loginResendEmail.value = ''
    showLoginPassword.value = false
    showAccountDeleted.value = false
    isRestoring.value = false
}

function switchToLogin() {
    activeTab.value = 'login'

    // 重置註冊表單欄位
    regAccount.value = ''
    regName.value = ''
    regEmail.value = ''
    regPassword.value = ''
    regConfirmPassword.value = ''
    // 重置註冊表單錯誤訊息
    regAccountError.value = ''
    regNameError.value = ''
    regEmailError.value = ''
    regPasswordError.value = ''
    regConfirmPasswordError.value = ''
    regFormError.value = ''
    // 重置眼睛開關
    showPassword.value = false
    showConfirmPassword.value = false
    // 重置成功狀態
    registerSuccess.value = false
}

// ── Demo 快速填入 ──
const isDev = import.meta.env.MODE === 'development'

const demoLoginAccounts = [
    { label: '一般會員', account: 'amy_chen', password: 'Aa000000' },
    { label: '黑名單會員', account: 'trouble_chen', password: 'Aa000000' },
    { label: '未驗證會員', account: 'amy', password: 'Aa000000' },
]

const demoRegisterData = {
    account: 'amy',
    name: 'Amy',
    email: 'chen.amy.eng@gmail.com',
    password: 'Aa000000',
    confirmPassword: 'Aa000000',
}

function fillDemoLogin(item) {
    loginAccount.value = item.account
    loginPassword.value = item.password
    loginFormError.value = ''
    showResendEmail.value = false
    showAccountDeleted.value = false
}

function fillDemoRegister() {
    regAccount.value = demoRegisterData.account
    regName.value = demoRegisterData.name
    regEmail.value = demoRegisterData.email
    regPassword.value = demoRegisterData.password
    regConfirmPassword.value = demoRegisterData.confirmPassword
    regAccountError.value = ''
    regNameError.value = ''
    regEmailError.value = ''
    regPasswordError.value = ''
    regConfirmPasswordError.value = ''
    regFormError.value = ''
}

onMounted(() => {
    const modalEl = document.querySelector('#authModal')

    modalEl.addEventListener('hidden.bs.modal', () => {
        // 登入表單重置
        loginAccount.value = ''
        loginPassword.value = ''
        loginFormError.value = ''
        loginSuccess.value = false
        showResendEmail.value = false
        loginResendEmail.value = ''
        showLoginPassword.value = false
        // 註冊表單重置
        regAccount.value = ''
        regName.value = ''
        regEmail.value = ''
        regPassword.value = ''
        regConfirmPassword.value = ''
        regAccountError.value = ''
        regNameError.value = ''
        regEmailError.value = ''
        regPasswordError.value = ''
        regConfirmPasswordError.value = ''
        regFormError.value = ''
        showPassword.value = false
        showConfirmPassword.value = false
        registerSuccess.value = false
        activeTab.value = 'login'
        showAccountDeleted.value = false
        isRestoring.value = false
    })
})
</script>

<template>
    <div id="authModal" class="modal fade" data-bs-backdrop="static" data-bs-keyboard="false" tabindex="-1" aria-labelledby="authModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered modal-fullscreen-sm-down modal-eat-dialog">
            <div class="modal-content modal-eat-content">
                <button type="button" class="btn-close btn-close-white btn-sm me-2 mt-2 ms-auto" data-bs-dismiss="modal" aria-label="關閉"></button>
                <!-- Header -->
                <div class="modal-header border-0 px-4 py-0">
                    <div class="w-100 d-flex justify-content-center">
                        <h2 id="authModalLabel" class="eat-h3 fw-bolder fst-normal fs-5 mb-0">
                            {{ activeTab === 'login' ? '會員登入' : '註冊會員' }}
                        </h2>
                    </div>
                </div>

                <!-- Tab 切換 -->
                <div class="px-4 pt-3 pb-2">
                    <div v-show="activeTab === 'login'">
                        <p class="text-center eat-body-muted mb-0 auth-footer-text">
                            還沒有帳號？
                            <button type="button" class="eat-link-btn" @click="switchToRegister">前往註冊</button>
                        </p>
                    </div>
                    <div v-show="activeTab === 'register'">
                        <p class="text-center eat-body-muted mb-0 auth-footer-text">
                            已有帳號？
                            <button type="button" class="eat-link-btn" @click="switchToLogin">前往登入</button>
                        </p>
                    </div>
                </div>

                <!-- Modal Body -->
                <div class="modal-body px-4 pt-2 pb-4" style="overflow-y: auto; max-height: 70vh">
                    <!-- ── 登入 Tab ── -->
                    <div v-show="activeTab === 'login'" class="form-eat">
                        <!-- 登入成功狀態 -->
                        <div v-if="loginSuccess" class="auth-success-box text-center py-3">
                            <div class="icon-eat icon-eat--success mb-2">
                                <i class="bi bi-check-lg"></i>
                            </div>
                            <p class="eat-body mb-1">登入成功</p>
                            <p class="eat-body-muted mb-0">歡迎回來，即將為您跳轉</p>
                        </div>

                        <!-- 登入表單 -->
                        <template v-else>
                            <div class="d-flex flex-column gap-2">
                                <Button variant="secondary" class="btn-eat-md mb-2" @click="handleGoogleLogin"> 使用 Google 登入 </Button>

                                <div class="auth-divider-content feather-divider on-container my-3"></div>

                                <!-- 通用錯誤橫幅 -->
                                <div v-if="loginFormError" class="auth-error-banner" role="alert">
                                    {{ loginFormError }}
                                </div>

                                <!-- 帳號已停用：重新啟用提示 -->
                                <div v-if="showAccountDeleted" class="auth-error-banner position-relative" role="alert">
                                    <button type="button" class="btn-close btn-close-white btn-sm position-absolute top-0 end-0 m-1" aria-label="關閉" @click="showAccountDeleted = false"></button>
                                    <p class="mb-2">此帳號已停用，是否要重新啟用？</p>
                                    <Button variant="primary" class="fs-6 py-1 w-100" :loading="isRestoring" @click="handleRestoreAccount">
                                        {{ isRestoring ? '啟用中...' : '重新啟用帳號' }}
                                    </Button>
                                </div>

                                <!-- Email 未驗證：補寄驗證信 -->
                                <div v-if="showResendEmail" class="d-flex flex-column gap-2">
                                    <p class="eat-body-muted mb-0" style="font-size: 0.85rem">輸入您的 Email，重新寄送驗證信：</p>
                                    <div class="d-flex gap-2">
                                        <input v-model="loginResendEmail" type="email" class="form-control flex-grow-1" placeholder="請輸入您的 Email" autocomplete="email" />
                                        <Button variant="secondary" class="btn-eat-md flex-shrink-0" :loading="isResendSubmitting" @click="handleResendVerifyEmail">
                                            {{ isResendSubmitting ? '寄送中...' : '重寄驗證信' }}
                                        </Button>
                                    </div>
                                </div>

                                <!-- Demo 快速填入（僅開發環境顯示） -->
                                <div v-if="isDev" class="demo-panel mb-1">
                                    <p class="demo-panel__label">⚡ DEMO — 點擊自動填入</p>
                                    <div class="d-flex flex-wrap gap-2">
                                        <button v-for="item in demoLoginAccounts" :key="item.account" type="button" class="demo-btn" @click="fillDemoLogin(item)">
                                            {{ item.label }}
                                        </button>
                                    </div>
                                </div>

                                <!-- 帳號 -->
                                <div>
                                    <label for="login-account" class="form-label d-block mb-1">帳號</label>
                                    <input id="login-account" v-model="loginAccount" type="text" class="form-control w-100" placeholder="請輸入帳號" autocomplete="off" />
                                </div>

                                <!-- 密碼 -->
                                <div>
                                    <label for="login-password" class="form-label d-block mb-1">密碼</label>
                                    <div class="position-relative">
                                        <input
                                            id="login-password"
                                            v-model="loginPassword"
                                            :type="showLoginPassword ? 'text' : 'password'"
                                            class="form-control w-100"
                                            placeholder="請輸入密碼"
                                            autocomplete="current-password"
                                        />
                                        <button
                                            type="button"
                                            class="btn-eat-password-toggle"
                                            @mousedown.prevent
                                            @click="showLoginPassword = !showLoginPassword"
                                            :aria-label="showLoginPassword ? '隱藏密碼' : '顯示密碼'"
                                        >
                                            <i :class="showLoginPassword ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
                                        </button>
                                    </div>
                                </div>

                                <Button variant="primary" class="btn-eat-md mt-3" :loading="isLoginSubmitting" @click="handleLogin">
                                    {{ isLoginSubmitting ? '登入中...' : '登入' }}
                                </Button>

                                <div class="text-center">
                                    <button type="button" class="eat-body-muted auth-small-link" style="background: none; border: none; padding: 0; cursor: pointer" @click="handleForgotPassword">
                                        忘記密碼？
                                    </button>
                                </div>
                            </div>
                        </template>
                    </div>

                    <!-- ── 註冊 Tab ── -->
                    <div v-show="activeTab === 'register'" class="form-eat">
                        <div class="d-flex flex-column gap-2">
                            <!-- 成功狀態 -->
                            <div v-if="registerSuccess" class="auth-success-box text-center py-3">
                                <div class="icon-eat icon-eat--success mb-2">
                                    <i class="bi bi-envelope"></i>
                                </div>
                                <p class="eat-body mb-1">驗證信已寄出</p>
                                <p class="eat-body-muted mb-0">請至信箱點擊驗證連結以完成註冊</p>
                            </div>

                            <!-- 註冊表單 -->
                            <template v-else>
                                <Button variant="secondary" class="btn-eat-md mb-2" @click="handleGoogleLogin"> 使用 Google 快速註冊 </Button>

                                <div class="auth-divider-content feather-divider on-container my-3"></div>

                                <!-- 通用錯誤橫幅 -->
                                <div v-if="regFormError" class="auth-error-banner" role="alert">
                                    {{ regFormError }}
                                </div>

                                <!-- Demo 快速填入（僅開發環境顯示） -->
                                <div v-if="isDev" class="demo-panel mb-1">
                                    <p class="demo-panel__label">⚡ DEMO — 點擊自動填入</p>
                                    <button type="button" class="demo-btn" @click="fillDemoRegister">示範填入</button>
                                </div>

                                <!-- 帳號 -->
                                <div>
                                    <label for="reg-account" class="form-label d-block mb-1">帳號</label>
                                    <input
                                        id="reg-account"
                                        v-model="regAccount"
                                        @blur="validateAccount"
                                        type="text"
                                        class="form-control w-100"
                                        :class="{ 'is-invalid': regAccountError }"
                                        placeholder="限英數字及底線，3–50 字元"
                                        autocomplete="username"
                                        :aria-describedby="regAccountError ? 'reg-account-error' : undefined"
                                        :aria-invalid="regAccountError ? 'true' : undefined"
                                    />
                                    <FormErrorMessage id="reg-account-error" :show="!!regAccountError" :message="regAccountError" />
                                </div>

                                <!-- 姓名 -->
                                <div>
                                    <label for="reg-name" class="form-label d-block mb-1">姓名</label>
                                    <input
                                        id="reg-name"
                                        v-model="regName"
                                        @blur="validateName"
                                        type="text"
                                        class="form-control w-100"
                                        :class="{ 'is-invalid': regNameError }"
                                        placeholder="請輸入您的姓名"
                                        autocomplete="name"
                                        :aria-describedby="regNameError ? 'reg-name-error' : undefined"
                                        :aria-invalid="regNameError ? 'true' : undefined"
                                    />
                                    <FormErrorMessage id="reg-name-error" :show="!!regNameError" :message="regNameError" />
                                </div>

                                <!-- Email -->
                                <div>
                                    <label for="reg-email" class="form-label d-block mb-1">Email</label>
                                    <input
                                        id="reg-email"
                                        v-model="regEmail"
                                        @blur="validateEmail"
                                        type="email"
                                        class="form-control w-100"
                                        :class="{ 'is-invalid': regEmailError }"
                                        placeholder="example@email.com"
                                        autocomplete="email"
                                        :aria-describedby="regEmailError ? 'reg-email-error' : undefined"
                                        :aria-invalid="regEmailError ? 'true' : undefined"
                                    />
                                    <FormErrorMessage id="reg-email-error" :show="!!regEmailError" :message="regEmailError" />
                                </div>

                                <!-- 密碼 -->
                                <div>
                                    <label for="reg-password" class="form-label d-block mb-1">密碼</label>
                                    <div class="position-relative">
                                        <input
                                            id="reg-password"
                                            v-model="regPassword"
                                            @blur="validatePassword"
                                            :type="showPassword ? 'text' : 'password'"
                                            class="form-control w-100"
                                            :class="{ 'is-invalid': regPasswordError }"
                                            placeholder="至少 8 個字元"
                                            autocomplete="new-password"
                                            :aria-describedby="regPasswordError ? 'reg-password-error' : undefined"
                                            :aria-invalid="regPasswordError ? 'true' : undefined"
                                        />
                                        <button
                                            type="button"
                                            class="btn-eat-password-toggle"
                                            @mousedown.prevent
                                            @click="showPassword = !showPassword"
                                            :aria-label="showPassword ? '隱藏密碼' : '顯示密碼'"
                                        >
                                            <i :class="showPassword ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
                                        </button>
                                    </div>

                                    <FormErrorMessage id="reg-password-error" :show="!!regPasswordError" :message="regPasswordError" />
                                </div>

                                <!-- 確認密碼 -->
                                <div>
                                    <label for="reg-confirm-password" class="form-label d-block mb-1">確認密碼</label>
                                    <div class="position-relative">
                                        <input
                                            id="reg-confirm-password"
                                            v-model="regConfirmPassword"
                                            @input="validateConfirmPassword"
                                            :type="showConfirmPassword ? 'text' : 'password'"
                                            class="form-control w-100"
                                            :class="{
                                                'is-invalid': regConfirmPasswordError,
                                            }"
                                            placeholder="再次輸入密碼"
                                            autocomplete="new-password"
                                            :aria-describedby="regConfirmPasswordError ? 'reg-confirm-password-error' : undefined"
                                            :aria-invalid="regConfirmPasswordError ? 'true' : undefined"
                                        />
                                        <button
                                            type="button"
                                            class="btn-eat-password-toggle"
                                            @click="showConfirmPassword = !showConfirmPassword"
                                            :aria-label="showConfirmPassword ? '隱藏密碼' : '顯示密碼'"
                                        >
                                            <i :class="showConfirmPassword ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
                                        </button>
                                    </div>
                                    <FormErrorMessage id="reg-confirm-password-error" :show="!!regConfirmPasswordError" :message="regConfirmPasswordError" />
                                </div>

                                <Button variant="primary" class="btn-eat-md mt-3 mb-4" :loading="isSubmitting" @click="handleRegister">
                                    {{ isSubmitting ? '註冊中...' : '註冊' }}
                                </Button>
                            </template>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
/* ── Error ── */
.auth-error-banner {
    background: var(--eat-error-container);
    color: var(--eat-error);
    padding: 0.65rem 1rem;
    border-radius: var(--eat-radius-sm);
    font-family: var(--font-label);
    font-size: 0.78rem;
    letter-spacing: 0.1em;
}

/* ── Divider ── */
.auth-divider-content.feather-divider::after {
    content: '或';
}

/* ── Link Button ── */
.eat-link-btn {
    background: none;
    border: none;
    color: var(--eat-primary);
    font-family: var(--font-body);
    font-style: italic;
    font-size: inherit;
    padding: 0;
    cursor: pointer;
    text-decoration: underline;
    text-underline-offset: 3px;
}

.eat-link-btn:hover {
    color: var(--eat-primary-fixed);
}

.auth-small-link {
    font-size: 0.85rem;
    text-decoration: underline;
    text-underline-offset: 3px;
}

.auth-footer-text {
    font-size: 0.85rem;
}

/* ── Success ── */
.auth-success-box {
    color: var(--eat-on-surface);
}

/* ── Demo Panel ── */
.demo-panel {
    background: color-mix(in srgb, var(--eat-primary) 10%, transparent);
    border: 1px dashed var(--eat-primary);
    border-radius: var(--eat-radius-sm);
    padding: 0.5rem 0.75rem;
}

.demo-panel__label {
    font-size: 0.72rem;
    letter-spacing: 0.08em;
    color: var(--eat-primary);
    margin-bottom: 0.4rem;
    font-family: var(--font-label);
}

.demo-btn {
    background: var(--eat-primary);
    color: var(--eat-on-primary, #1a1200);
    border: none;
    border-radius: var(--eat-radius-sm);
    padding: 0.25rem 0.75rem;
    font-size: 0.78rem;
    font-family: var(--font-label);
    cursor: pointer;
    transition: var(--eat-transition);
}

.demo-btn:hover {
    opacity: 0.85;
}
</style>
