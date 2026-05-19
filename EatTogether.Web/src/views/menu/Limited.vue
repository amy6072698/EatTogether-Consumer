<template>
    <div class="limited-page">
        <!-- ═══════════════════════════════════════════
       Header
    ════════════════════════════════════════════ -->
        <header class="limited-header">
            <div class="header-bg" ref="headerBgRef"></div>
            <div class="header-overlay"></div>
            <div class="container header-content">
                <span class="limited-eyebrow">Limited Edition</span>
                <h1 class="eat-h1">本季限定</h1>
                <p class="limited-subtitle">精選時令食材，限時呈獻，錯過不再</p>
            </div>
        </header>

        <main class="container py-main">
            <!-- Loading -->
            <div v-if="loading" class="state-container">
                <div class="spinner"></div>
                <p class="loading-text">正在為您準備限定佳餚...</p>
            </div>

            <!-- Error -->
            <div v-else-if="error" class="state-container">
                <p class="error-msg">{{ error }}</p>
                <button class="retry-btn" @click="fetchLimited">重新嘗試</button>
            </div>

            <!-- Empty -->
            <div v-else-if="limitedDishes.length === 0" class="state-container empty-state">
                <!-- 沙漏 SVG -->
                <svg
                    class="empty-icon"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 64 80"
                    fill="none"
                >
                    <rect
                        x="10"
                        y="6"
                        width="44"
                        height="5"
                        rx="2.5"
                        stroke="currentColor"
                        stroke-width="1.8"
                    />
                    <rect
                        x="10"
                        y="69"
                        width="44"
                        height="5"
                        rx="2.5"
                        stroke="currentColor"
                        stroke-width="1.8"
                    />
                    <path
                        d="M14 11 C14 11 14 32 32 40 C50 48 50 69 50 69"
                        stroke="currentColor"
                        stroke-width="1.8"
                        stroke-linecap="round"
                    />
                    <path
                        d="M50 11 C50 11 50 32 32 40 C14 48 14 69 14 69"
                        stroke="currentColor"
                        stroke-width="1.8"
                        stroke-linecap="round"
                    />
                    <ellipse cx="32" cy="54" rx="8" ry="4" fill="currentColor" opacity="0.25" />
                    <circle cx="32" cy="40" r="2" fill="currentColor" opacity="0.5" />
                </svg>
                <p class="empty-title">目前沒有限定供應餐點</p>
                <p class="empty-sub">敬請期待下一波限定菜單</p>
            </div>

            <!-- 統計列 -->
            <p v-else class="stats-bar">
                本季共 <strong>{{ limitedDishes.length }}</strong> 道限定餐點
                <span v-if="endingSoon > 0" class="ending-warn">・{{ endingSoon }} 道即將截止</span>
            </p>

            <!-- 近期異動預告 -->
            <div v-if="!loading && !error && hasChanges" class="changes-preview">
                <!-- 即將上架 -->
                <div v-if="upcomingDishes.length" class="changes-row">
                    <span class="changes-label label-upcoming">
                        <svg
                            width="11"
                            height="11"
                            viewBox="0 0 12 12"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="1.6"
                            stroke-linecap="round"
                        >
                            <circle cx="6" cy="6" r="5" />
                            <polyline points="6 3 6 6 8.5 7.5" />
                        </svg>
                        即將上架
                    </span>
                    <div class="changes-chips">
                        <span
                            v-for="d in upcomingDishes"
                            :key="d.id"
                            class="change-chip chip-upcoming"
                        >
                            {{ d.dishName }}
                            <em>{{ daysUntil(d.startDate) }} 天後上架</em>
                        </span>
                    </div>
                </div>

                <!-- 即將下架 -->
                <div v-if="expiringSoonDishes.length" class="changes-row">
                    <span class="changes-label label-expiring">
                        <svg
                            width="11"
                            height="11"
                            viewBox="0 0 12 12"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="1.6"
                            stroke-linecap="round"
                        >
                            <circle cx="6" cy="6" r="5" />
                            <line x1="6" y1="3" x2="6" y2="6.5" />
                            <line x1="6" y1="8.5" x2="6" y2="9" />
                        </svg>
                        即將下架
                    </span>
                    <div class="changes-chips">
                        <span
                            v-for="d in expiringSoonDishes"
                            :key="d.id"
                            class="change-chip chip-expiring"
                        >
                            {{ d.dishName }}
                            <em>剩 {{ daysUntil(d.endDate) }} 天</em>
                        </span>
                    </div>
                </div>
            </div>

            <!-- 卡片格 -->
            <TransitionGroup
                v-if="!loading && !error && limitedDishes.length > 0"
                name="card"
                tag="div"
                class="limited-grid"
            >
                <div
                    v-for="(dish, index) in limitedDishes"
                    :key="dish.id"
                    v-reveal="index"
                    class="limited-card"
                    :class="{
                        'is-soldout': dish.stockStatus === 2,
                        'is-upcoming': isUpcoming(dish),
                    }"
                    :style="{ '--glow-delay': `${(index % 3) * 1.4}s` }"
                    @click="openModal(dish)"
                    @mousemove="dish.stockStatus !== 2 && !isUpcoming(dish) && onCardMove($event)"
                    @mouseleave="activePreview = null"
                >
                    <!-- ── 圖片區 ── -->
                    <div class="card-img-wrap">
                        <img
                            v-if="dish.imageUrl"
                            :src="formatImageUrl(dish.imageUrl)"
                            :alt="dish.dishName"
                            loading="lazy"
                        />
                        <div v-else class="img-placeholder">
                            <span>{{ dish.dishName.charAt(0) }}</span>
                        </div>

                        <!-- 售完遮罩 -->
                        <div v-if="dish.stockStatus === 2" class="soldout-overlay">
                            <span class="soldout-text">已售完</span>
                        </div>

                        <!-- 即將上架遮罩 -->
                        <div v-else-if="isUpcoming(dish)" class="upcoming-overlay">
                            <svg
                                width="22"
                                height="22"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                stroke-width="1.8"
                                stroke-linecap="round"
                            >
                                <circle cx="12" cy="12" r="10" />
                                <polyline points="12 6 12 12 16 14" />
                            </svg>
                            <span>即將上架</span>
                        </div>

                        <!-- 庫存條 -->
                        <div
                            v-if="dish.stockStatus === 1"
                            class="stock-bar"
                            :style="{ width: stockWidth(dish.id) + '%' }"
                        ></div>

                        <!-- 徽章群 -->
                        <div class="badge-group">
                            <span class="badge badge-lim">✦ 限定</span>
                            <span v-if="dish.isRecommended" class="badge badge-rec">推薦</span>
                            <span v-if="dish.isPopular" class="badge badge-pop">熱銷</span>
                            <span v-if="dish.stockStatus === 1" class="badge badge-low"
                                >即將售完</span
                            >
                        </div>

                        <!-- 倒數徽章（右上角） -->
                        <div
                            v-if="dish.endDate && !isUpcoming(dish) && dish.stockStatus !== 2"
                            class="countdown-badge"
                            :class="{ urgent: isUrgent(dish.endDate) }"
                        >
                            {{ getCountdown(dish.endDate) }}
                        </div>

                        <!-- 收藏按鈕 -->
                        <FavoriteButton
                            :dish-id="dish.id"
                            :is-favorited="favorites.includes(dish.id)"
                            @toggle="toggleFavorite"
                        />
                        <!-- 快速預覽按鈕 -->
                        <button
                            v-if="dish.stockStatus !== 2 && !isUpcoming(dish)"
                            class="preview-btn"
                            @click.stop="activePreview = activePreview === dish.id ? null : dish.id"
                            aria-label="快速預覽"
                        >
                            👁
                        </button>
                    </div>

                    <!-- 快速預覽 Tooltip -->
                    <Transition name="preview-tip">
                        <div v-if="activePreview === dish.id" class="preview-tip" @click.stop>
                            <p class="tip-desc">
                                {{
                                    dish.description ||
                                    '限定季節食材，主廚嚴選推薦，每一口都是時令的珍貴。'
                                }}
                            </p>
                            <div
                                v-if="parseIngredients(dish.ingredientsJson).length"
                                class="tip-ingredients"
                            >
                                <span
                                    v-for="ing in parseIngredients(dish.ingredientsJson).slice(
                                        0,
                                        3
                                    )"
                                    :key="ing.name"
                                    class="tip-ing-chip"
                                    >{{ ing.name }}</span
                                >
                            </div>
                            <button class="tip-more" @click.stop="openModal(dish)">
                                查看完整資訊 →
                            </button>
                        </div>
                    </Transition>

                    <!-- ── 資訊區 ── -->
                    <div class="card-info">
                        <div class="title-row">
                            <h3 class="card-name">{{ dish.dishName }}</h3>
                            <span class="card-price">NT$ {{ dish.price.toLocaleString() }}</span>
                        </div>

                        <p class="card-desc">
                            {{
                                dish.description ||
                                '限定季節食材，主廚嚴選推薦，每一口都是時令的珍貴。'
                            }}
                        </p>

                        <!-- 供應期間 -->
                        <div class="period-row">
                            <svg
                                class="period-icon"
                                viewBox="0 0 16 16"
                                fill="none"
                                stroke="currentColor"
                                stroke-width="1.4"
                                stroke-linecap="round"
                            >
                                <rect x="2" y="3" width="12" height="11" rx="1.5" />
                                <line x1="5" y1="1.5" x2="5" y2="4.5" />
                                <line x1="11" y1="1.5" x2="11" y2="4.5" />
                                <line x1="2" y1="7" x2="14" y2="7" />
                            </svg>
                            <span class="period-text">
                                {{ formatDate(dish.startDate) }}
                                <span class="period-sep">—</span>
                                {{ dish.endDate ? formatDate(dish.endDate) : '供應至售完' }}
                            </span>
                        </div>

                        <!-- 標籤 -->
                        <div class="card-tags">
                            <span v-if="dish.isVegetarian" class="tag tag-veg">🥬 素食</span>
                            <span v-if="dish.spicyLevel > 0" class="tag tag-spicy">{{
                                '🌶️'.repeat(dish.spicyLevel)
                            }}</span>
                            <span v-if="dish.isTakeOut" class="tag tag-take">🛍 外帶</span>
                        </div>
                    </div>
                </div>
            </TransitionGroup>
        </main>

        <ToastContainer />

        <!-- ── DEV 專用：手動觸發提醒信 ── -->
        <button
            v-if="isDev"
            class="dev-remind-btn"
            :disabled="isSendingReminders"
            @click="triggerSendReminders"
        >
            {{ isSendingReminders ? '發送中...' : '📧 發送到期提醒信' }}
            <span class="dev-label">展示用</span>
        </button>

        <!-- ── Modal ── -->
        <Transition name="modal">
            <div v-if="isModalOpen && selectedDish" class="modal-overlay" @click.self="closeModal">
                <div class="modal-box">
                    <!-- 圖片區 -->
                    <div class="modal-img-wrap">
                        <img
                            v-if="selectedDish.imageUrl"
                            :src="formatImageUrl(selectedDish.imageUrl)"
                            :alt="selectedDish.dishName"
                            class="modal-img"
                            ref="modalImgRef"
                        />
                        <div v-else class="modal-img-placeholder">
                            <span>{{ selectedDish.dishName.charAt(0) }}</span>
                        </div>
                        <div class="modal-img-gradient"></div>
                        <!-- URL 由父層 openShareItem 動態組裝，此處傳空值為預留介面 -->
                        <ShareMenu
                            v-model="shareMenuOpen"
                            share-url=""
                            share-title=""
                            @select="openShareItem"
                        />
                        <button class="modal-close" @click="closeModal">✕</button>
                        <div class="modal-badge-group">
                            <span class="badge badge-lim">✦ 限定</span>
                            <span v-if="selectedDish.isRecommended" class="badge badge-rec"
                                >推薦</span
                            >
                            <span v-if="selectedDish.isPopular" class="badge badge-pop">熱銷</span>
                        </div>
                    </div>

                    <!-- 資訊區 -->
                    <div class="modal-body" ref="modalBodyRef">
                        <!-- 標題 + 價格 -->
                        <div class="modal-title-row">
                            <h2 class="modal-title">{{ selectedDish.dishName }}</h2>
                            <span class="modal-price"
                                >NT$ {{ selectedDish.price.toLocaleString() }}</span
                            >
                        </div>

                        <!-- 收藏按鈕 -->
                        <FavoriteButton
                            :dish-id="selectedDish.id"
                            :is-favorited="favorites.includes(selectedDish.id)"
                            @toggle="toggleFavorite"
                        />

                        <!-- 描述 -->
                        <p class="modal-desc">
                            {{
                                selectedDish.description ||
                                '限定季節食材，主廚嚴選推薦，每一口都是時令的珍貴。'
                            }}
                        </p>

                        <!-- 倒數計時（供應中才顯示）-->
                        <div
                            v-if="
                                selectedDish.endDate &&
                                !isUpcoming(selectedDish) &&
                                selectedDish.stockStatus !== 2
                            "
                            class="modal-countdown"
                            :class="{ urgent: isUrgent(selectedDish.endDate) }"
                        >
                            <span class="modal-countdown-label">距離截止</span>
                            <span class="modal-countdown-value">{{
                                getCountdown(selectedDish.endDate)
                            }}</span>
                        </div>

                        <!-- 提醒按鈕（有 endDate 且供應中才顯示）-->
                        <button
                            v-if="
                                selectedDish.endDate &&
                                !isUpcoming(selectedDish) &&
                                selectedDish.stockStatus !== 2
                            "
                            class="modal-remind-btn"
                            :class="{ 'is-set': reminders.includes(selectedDish.id) }"
                            @click="toggleReminder(selectedDish)"
                        >
                            {{
                                reminders.includes(selectedDish.id)
                                    ? '✓ 已設定提醒（點擊取消）'
                                    : '🔔 提醒我'
                            }}
                        </button>

                        <!-- 供應期間 -->
                        <div class="modal-period">
                            <svg
                                class="modal-period-icon"
                                viewBox="0 0 16 16"
                                fill="none"
                                stroke="currentColor"
                                stroke-width="1.4"
                                stroke-linecap="round"
                            >
                                <rect x="2" y="3" width="12" height="11" rx="1.5" />
                                <line x1="5" y1="1.5" x2="5" y2="4.5" />
                                <line x1="11" y1="1.5" x2="11" y2="4.5" />
                                <line x1="2" y1="7" x2="14" y2="7" />
                            </svg>
                            <div>
                                <div class="modal-period-label">供應期間</div>
                                <div class="modal-period-dates">
                                    {{ formatDate(selectedDish.startDate) }}
                                    <span style="opacity: 0.5; margin: 0 0.4em">—</span>
                                    {{
                                        selectedDish.endDate
                                            ? formatDate(selectedDish.endDate)
                                            : '供應至售完'
                                    }}
                                </div>
                            </div>
                        </div>

                        <!-- 屬性 chips -->
                        <div class="modal-attrs">
                            <span class="attr-chip" v-if="selectedDish.categoryName">{{
                                selectedDish.categoryName
                            }}</span>
                            <span class="attr-chip veg" v-if="selectedDish.isVegetarian"
                                >🥬 素食</span
                            >
                            <span class="attr-chip spicy" v-if="selectedDish.spicyLevel > 0"
                                >{{ '🌶️'.repeat(selectedDish.spicyLevel) }}
                                {{ spicyLabel(selectedDish.spicyLevel) }}</span
                            >
                            <span class="attr-chip take" v-if="selectedDish.isTakeOut"
                                >🛍 外帶</span
                            >
                            <span class="attr-chip rec" v-if="selectedDish.isRecommended"
                                >⭐ 主廚推薦</span
                            >
                            <span class="attr-chip pop" v-if="selectedDish.isPopular">🔥 熱銷</span>
                        </div>

                        <!-- 前往評分 -->
                        <button class="modal-rate-btn" @click="goToRating">✍ 前往評分</button>
                    </div>
                </div>
            </div>
        </Transition>
    </div>
</template>

<script setup>
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Modal } from 'bootstrap'
import ToastContainer from '@/components/common/ToastContainer.vue'
import { useToast } from '@/composables/useToast.js'
import apiFetch from '@/utils/apiFetch.js'
import { useAuthStore } from '@/stores/auth.js'
import ShareMenu from '@/components/common/menu/ShareMenu.vue'
import FavoriteButton from '@/components/common/menu/FavoriteButton.vue'
const { show } = useToast()
const authStore = useAuthStore()
const route = useRoute()
const router = useRouter()

// ── Intersection Observer 進場動畫 ───────────────────
const vReveal = {
    mounted(el, binding) {
        const delay = binding.value * 60
        el.style.opacity = '0'
        el.style.transform = 'translateY(36px)'
        el.style.transition = `opacity 0.4s cubic-bezier(0.22,1,0.36,1), transform 0.4s cubic-bezier(0.22,1,0.36,1)`
        el.style.transitionDelay = `${delay}ms`
        const observer = new IntersectionObserver(
            ([entry]) => {
                if (entry.isIntersecting) {
                    el.style.opacity = '1'
                    el.style.transform = 'translateY(0)'
                    setTimeout(() => {
                        el.style.opacity = ''
                        el.style.transform = ''
                        el.style.transition = ''
                        el.style.transitionDelay = ''
                        el.classList.add('is-revealed')
                    }, 400 + delay)
                    observer.disconnect()
                }
            },
            { threshold: 0.1 }
        )
        observer.observe(el)
        el._revealObserver = observer
    },
    unmounted(el) {
        el._revealObserver?.disconnect()
    },
}

// ── 工具函式 ─────────────────────────────────────────
const onCardMove = (e) => {
    const card = e.currentTarget
    const rect = card.getBoundingClientRect()
    card.style.setProperty('--mx', `${(((e.clientX - rect.left) / rect.width) * 100).toFixed(1)}%`)
    card.style.setProperty('--my', `${(((e.clientY - rect.top) / rect.height) * 100).toFixed(1)}%`)
}

const stockWidth = (id) => {
    const n =
        typeof id === 'number'
            ? id
            : String(id)
                  .split('')
                  .reduce((a, c) => a + c.charCodeAt(0), 0)
    return 20 + (n % 16)
}

const formatImageUrl = (url) => {
    if (!url) return null
    return url
}

const formatDate = (dateStr) => {
    if (!dateStr) return '—'
    const d = new Date(dateStr)
    return `${d.getFullYear()}/${String(d.getMonth() + 1).padStart(2, '0')}/${String(d.getDate()).padStart(2, '0')}`
}

// ── 即時時鐘（每秒更新） ─────────────────────────────
const currentTime = ref(new Date())
let _clockTimer = null

const getCountdown = (endDateStr) => {
    if (!endDateStr) return ''
    const end = new Date(endDateStr)
    const now = currentTime.value
    if (end <= now) return '已結束'
    const totalSec = Math.floor((end - now) / 1000)
    const days = Math.floor(totalSec / 86400)
    if (days >= 1) return `剩 ${days} 天`
    const h = Math.floor(totalSec / 3600)
    const m = Math.floor((totalSec % 3600) / 60)
    const s = totalSec % 60
    return `剩 ${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
}

// startDate 在今天之後 → 即將上架；否則以 isActive 兜底
const isUpcoming = (dish) => {
    if (dish.startDate) return new Date(dish.startDate) > currentTime.value
    return !dish.isActive
}

// 3 天內視為「緊迫」→ urgent 樣式
const isUrgent = (endDateStr) => {
    if (!endDateStr) return false
    const diff = new Date(endDateStr) - currentTime.value
    return diff > 0 && diff < 3 * 24 * 60 * 60 * 1000
}

// ── 辣度標籤 ─────────────────────────────────────────
const spicyLabel = (level) => ['', '微辣', '中辣', '大辣', '極辣'][level] ?? '辣'

// ── 收藏 ─────────────────────────────────────────────
const favorites = ref([])

const loadFavorites = async () => {
    if (authStore.isLoggedIn) {
        try {
            const res = await apiFetch('/Favorites')
            if (res.ok) {
                favorites.value = await res.json()
                return
            }
        } catch {
            /* fall through */
        }
    }
    favorites.value = []
}

const toggleFavorite = async (dishId) => {
    if (!authStore.isLoggedIn) {
        show('請先登入才能收藏', 'info')
        Modal.getOrCreateInstance(document.querySelector('#authModal')).show()
        return
    }
    const isFav = favorites.value.includes(dishId)
    try {
        const res = await apiFetch(`/Favorites/${dishId}`, { method: isFav ? 'DELETE' : 'POST' })
        if (!res.ok) throw new Error()
        if (isFav) {
            favorites.value = favorites.value.filter((id) => id !== dishId)
            show('🤍 已取消收藏', 'info')
        } else {
            favorites.value.push(dishId)
            show('❤️ 已加入收藏', 'success')
        }
    } catch {
        show('操作失敗，請稍後再試', 'error')
    }
}

// 登入後同步 localStorage 收藏到伺服器
watch(
    () => authStore.isLoggedIn,
    async (loggedIn) => {
        if (loggedIn) {
            await loadFavorites()
            await loadReminders()
        } else {
            favorites.value = []
            reminders.value = []
        }
    }
)

// ── 提醒 ─────────────────────────────────────────────
const reminders = ref([])

const loadReminders = async () => {
    if (authStore.isLoggedIn) {
        try {
            const res = await apiFetch('/LimitedNotifications')
            if (res.ok) {
                reminders.value = await res.json()
                return
            }
        } catch {
            /* fall through */
        }
    }
    reminders.value = []
}

const toggleReminder = async (dish) => {
    if (!authStore.isLoggedIn) {
        show('請先登入才能設定提醒', 'info')
        Modal.getOrCreateInstance(document.querySelector('#authModal')).show()
        return
    }
    const isSet = reminders.value.includes(dish.id)
    try {
        const res = await apiFetch(`/LimitedNotifications/${dish.id}`, {
            method: isSet ? 'DELETE' : 'POST',
        })
        if (!res.ok) throw new Error()
        if (isSet) {
            reminders.value = reminders.value.filter((id) => id !== dish.id)
            show('提醒已取消', 'info')
        } else {
            reminders.value.push(dish.id)
            show('🔔 提醒已設定！到期前將以 Email 通知您', 'success')
        }
    } catch {
        show('操作失敗，請稍後再試', 'error')
    }
}

// ── DEV：手動觸發提醒信 ─────────────────────────────
const isDev = import.meta.env.DEV
const isSendingReminders = ref(false)

const triggerSendReminders = async () => {
    isSendingReminders.value = true
    try {
        const res = await apiFetch('/LimitedNotifications/SendReminders', { method: 'POST' })
        if (!res.ok) throw new Error()
        show('提醒信已發送！', 'success')
    } catch {
        show('發送失敗，請稍後再試', 'error')
    } finally {
        isSendingReminders.value = false
    }
}

// ── 食材 JSON 解析 ────────────────────────────────────
const parseIngredients = (json) => {
    try {
        const arr = JSON.parse(json)
        return Array.isArray(arr) ? arr : []
    } catch {
        return []
    }
}

// ── Modal ─────────────────────────────────────────────
const isModalOpen = ref(false)
const activePreview = ref(null)
const selectedDish = ref(null)
const modalBodyRef = ref(null)
const modalImgRef = ref(null)

watch(isModalOpen, async (open) => {
    if (!open) return
    await nextTick()

    // B: 淡入
    const img = modalImgRef.value
    if (img) {
        img.style.transition = 'none'
        img.style.opacity = '0'
        img.offsetHeight
        img.style.transition = 'opacity 0.6s ease'
        img.style.opacity = '1'
    }

    const body = modalBodyRef.value
    if (!body) return

    const sections = [...body.children]

    // D: pre-hide chips
    const chips = body.querySelectorAll('.attr-chip')
    chips.forEach((chip) => {
        chip.style.transition = 'none'
        chip.style.opacity = '0'
        chip.style.transform = 'scale(0) translateY(6px)'
    })

    // A: reset sections
    sections.forEach((el) => {
        el.style.transition = 'none'
        el.style.opacity = '0'
        el.style.transform = 'translateY(30px) scale(0.96)'
    })

    body.offsetHeight

    // A: stagger in
    sections.forEach((el, i) => {
        setTimeout(() => {
            el.style.transition =
                'opacity 0.55s cubic-bezier(0.22, 1, 0.36, 1), transform 0.55s cubic-bezier(0.22, 1, 0.36, 1)'
            el.style.opacity = '1'
            el.style.transform = 'translateY(0) scale(1)'
        }, i * 90)
    })

    // D: chips bounce in
    const chipsDelay = (sections.length - 1) * 90 + 180
    chips.forEach((chip, i) => {
        setTimeout(
            () => {
                chip.style.transition =
                    'transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.25s ease'
                chip.style.opacity = '1'
                chip.style.transform = 'scale(1) translateY(0)'
            },
            chipsDelay + i * 55
        )
    })
})

const shareMenuOpen = ref(false)

const openShareItem = async (type) => {
    const dish = selectedDish.value
    if (!dish) return
    const dishUrl = `${window.location.origin}/limited?dish=${dish.id}`
    const encodedUrl = encodeURIComponent(dishUrl)
    const encodedText = encodeURIComponent(`${dish.dishName} NT$${dish.price.toLocaleString()}`)
    switch (type) {
        case 'line':
            window.open(`https://social-plugins.line.me/lineit/share?url=${encodedUrl}`, '_blank')
            break
        case 'facebook':
            window.open(`https://www.facebook.com/sharer/sharer.php?u=${encodedUrl}`, '_blank')
            break
        case 'x':
            window.open(
                `https://twitter.com/intent/tweet?text=${encodedText}&url=${encodedUrl}`,
                '_blank'
            )
            break
        case 'copy':
            try {
                await navigator.clipboard.writeText(dishUrl)
                show('🔗 連結已複製！', 'success')
            } catch {
                show('複製失敗，請手動複製', 'error')
            }
            break
    }
    shareMenuOpen.value = false
}

const openModal = (dish) => {
    // 售完或即將上架 → 不開 Modal
    if (dish.stockStatus === 2 || isUpcoming(dish)) return
    selectedDish.value = dish
    isModalOpen.value = true
    document.body.style.overflow = 'hidden'
}
const closeModal = () => {
    isModalOpen.value = false
    selectedDish.value = null
    shareMenuOpen.value = false
    document.body.style.overflow = ''
}

const goToRating = () => {
    const id = selectedDish.value?.id
    if (!id) return
    closeModal()
    router.push(`/menu?dish=${id}&returnTo=/limited`)
}
const handleEsc = (e) => {
    if (e.key === 'Escape') closeModal()
}

// ── State ─────────────────────────────────────────────
const dishes = ref([])
const loading = ref(true)
const error = ref(null)
let _refreshTimer = null

// ── Computed ──────────────────────────────────────────
const limitedDishes = computed(() =>
    dishes.value
        .filter((d) => d.isLimited)
        .sort((a, b) => {
            // 0=上架中, 1=售完, 2=即將上架
            const rank = (d) => {
                if (isUpcoming(d)) return 2
                if (d.stockStatus === 2) return 1
                return 0
            }
            return rank(a) - rank(b)
        })
)

// 3 天內即將截止的數量（stats-bar 用）
const endingSoon = computed(
    () =>
        limitedDishes.value.filter(
            (d) => d.endDate && isUrgent(d.endDate) && d.isActive && d.stockStatus !== 2
        ).length
)

// 距離日期還剩幾天（無條件進位，最小 1）
const daysUntil = (dateStr) => {
    if (!dateStr) return 0
    const diff = new Date(dateStr) - currentTime.value
    return Math.max(1, Math.ceil(diff / (1000 * 60 * 60 * 24)))
}

// 即將上架：startDate 在 1~7 天後且 isActive=false
const upcomingDishes = computed(() =>
    limitedDishes.value.filter((d) => {
        if (d.isActive || !d.startDate) return false
        const days = (new Date(d.startDate) - currentTime.value) / (1000 * 60 * 60 * 24)
        return days >= 0 && days <= 7
    })
)

// 即將下架：endDate 在 1~7 天後且 isActive=true、未售完
const expiringSoonDishes = computed(() =>
    limitedDishes.value.filter((d) => {
        if (!d.isActive || d.stockStatus === 2 || !d.endDate) return false
        const days = (new Date(d.endDate) - currentTime.value) / (1000 * 60 * 60 * 24)
        return days >= 0 && days <= 7
    })
)

// 兩個子區塊任一有資料才顯示整個公告列
const hasChanges = computed(
    () => upcomingDishes.value.length > 0 || expiringSoonDishes.value.length > 0
)

// ── API ───────────────────────────────────────────────
const fetchLimited = async () => {
    if (dishes.value.length === 0) loading.value = true
    try {
        const res = await apiFetch('/Dishes/active')
        if (!res.ok) throw new Error(`抓取失敗 (${res.status})`)
        dishes.value = await res.json()
        error.value = null
    } catch (err) {
        console.error('Limited Fetch Error:', err)
        if (dishes.value.length === 0) error.value = '無法載入限定餐點，請確認 API 伺服器狀態。'
    } finally {
        loading.value = false
    }
}

// ── 生命週期 ──────────────────────────────────────────
const headerBgRef = ref(null)
const handleParallax = () => {
    if (headerBgRef.value) {
        headerBgRef.value.style.transform = `translateY(${window.scrollY * 0.4}px)`
    }
}

onMounted(async () => {
    await fetchLimited()
    await loadFavorites()
    await loadReminders()
    _refreshTimer = setInterval(fetchLimited, 5_000)
    _clockTimer = setInterval(() => {
        currentTime.value = new Date()
    }, 1000)
    window.addEventListener('keydown', handleEsc)
    window.addEventListener('scroll', handleParallax, { passive: true })

    // 深層連結：偵測 ?dish=id，自動打開對應 Modal
    const dishParam = route.query.dish
    if (dishParam) {
        const dish = limitedDishes.value.find((d) => String(d.id) === dishParam)
        if (dish) openModal(dish)
    }

    // 提醒通知：檢查已設定提醒且距截止 24h 內的餐點
    if (Notification.permission === 'granted' && reminders.value.length) {
        const now = new Date()
        reminders.value.forEach((id) => {
            const dish = limitedDishes.value.find((d) => d.id === id)
            if (!dish || !dish.endDate) return
            const end = new Date(dish.endDate)
            const diffMs = end - now
            if (diffMs > 0 && diffMs < 24 * 60 * 60 * 1000) {
                const h = Math.floor(diffMs / 3_600_000)
                const m = Math.floor((diffMs % 3_600_000) / 60_000)
                new Notification(`⏰ ${dish.dishName} 即將截止！`, {
                    body: `距離下架還有 ${h} 小時 ${m} 分鐘，把握機會！`,
                    icon: dish.imageUrl ? dish.imageUrl : undefined,
                })
            }
        })
    }
})
onUnmounted(() => {
    clearInterval(_refreshTimer)
    clearInterval(_clockTimer)
    window.removeEventListener('keydown', handleEsc)
    window.removeEventListener('scroll', handleParallax)
    document.body.style.overflow = ''
})
</script>

<style scoped>
/* ── 全域基礎 ─────────────────────────────────────── */
.limited-page {
    background-color: var(--eat-surface);
    min-height: 100vh;
    color: var(--eat-on-surface);
    font-family: var(--font-body);
}

/* ── Header ──────────────────────────────────────── */
.limited-header {
    position: relative;
    padding: 6rem 0 2.5rem;
    text-align: center;
    overflow: hidden;
}
.header-bg {
    position: absolute;
    inset: 0;
    background-image: url('https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=1400&q=80');
    background-size: cover;
    background-position: center 40%;
    will-change: transform;
}
.header-overlay {
    position: absolute;
    inset: 0;
    background: linear-gradient(
        to bottom,
        rgba(24, 11, 6, 0.82) 0%,
        rgba(24, 11, 6, 0.72) 60%,
        var(--eat-surface) 100%
    );
}
.header-content {
    position: relative;
    z-index: 2;
    top: -1.5rem;
}

.limited-eyebrow {
    display: block;
    font-family: var(--font-label);
    color: var(--eat-secondary);
    letter-spacing: 0.45em;
    text-transform: uppercase;
    font-size: 0.72rem;
    margin-bottom: 1rem;
}

.eat-h1 {
    font-family: var(--font-headline);
    font-size: clamp(2.5rem, 5vw, 3.5rem);
    color: var(--eat-primary);
    font-style: italic;
    margin-bottom: 1rem;
}

.limited-subtitle {
    font-family: var(--font-body);
    font-size: clamp(0.9rem, 2vw, 1.05rem);
    color: rgba(249, 221, 211, 0.55);
    font-style: italic;
    letter-spacing: 0.06em;
    margin: 0;
}

/* ── 統計列 ──────────────────────────────────────── */
.stats-bar {
    font-family: var(--font-label);
    font-size: 0.75rem;
    color: rgba(249, 221, 211, 0.4);
    letter-spacing: 0.15em;
    margin-bottom: 2rem;
}
.stats-bar strong {
    color: var(--eat-primary);
    font-weight: 600;
}
.ending-warn {
    color: #f97316;
    font-weight: 600;
    letter-spacing: 0.05em;
    animation: warn-pulse 2s ease-in-out infinite;
}
@keyframes warn-pulse {
    0%,
    100% {
        opacity: 1;
    }
    50% {
        opacity: 0.65;
    }
}

/* ── 近期異動預告 ─────────────────────────────────── */
.changes-preview {
    display: flex;
    flex-direction: column;
    gap: 0.55rem;
    margin-bottom: 2rem;
    padding: 0.9rem 1.2rem;
    background: rgba(255, 255, 255, 0.025);
    border: 1px solid rgba(227, 199, 107, 0.1);
    border-radius: 12px;
}

.changes-row {
    display: flex;
    align-items: center;
    gap: 0.85rem;
    min-width: 0;
}

.changes-label {
    display: inline-flex;
    align-items: center;
    gap: 0.3rem;
    font-family: var(--font-label);
    font-size: 0.62rem;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    white-space: nowrap;
    padding: 0.22rem 0.65rem;
    border-radius: 20px;
    flex-shrink: 0;
}
.label-upcoming {
    color: var(--eat-primary);
    background: rgba(227, 199, 107, 0.1);
    border: 1px solid rgba(227, 199, 107, 0.3);
}
.label-expiring {
    color: #fb923c;
    background: rgba(249, 115, 22, 0.1);
    border: 1px solid rgba(249, 115, 22, 0.35);
}

/* Chip 橫向捲動列 */
.changes-chips {
    display: flex;
    gap: 0.45rem;
    overflow-x: auto;
    scrollbar-width: none;
    -ms-overflow-style: none;
    padding-bottom: 2px;
    min-width: 0;
}
.changes-chips::-webkit-scrollbar {
    display: none;
}

.change-chip {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    white-space: nowrap;
    font-family: var(--font-label);
    font-size: 0.72rem;
    padding: 0.22rem 0.75rem;
    border-radius: 20px;
    flex-shrink: 0;
}
.change-chip em {
    font-style: normal;
    opacity: 0.75;
    font-size: 0.65rem;
}
.chip-upcoming {
    background: rgba(227, 199, 107, 0.07);
    border: 1px solid rgba(227, 199, 107, 0.2);
    color: rgba(249, 221, 211, 0.75);
}
.chip-upcoming em {
    color: var(--eat-primary);
}

.chip-expiring {
    background: rgba(249, 115, 22, 0.07);
    border: 1px solid rgba(249, 115, 22, 0.25);
    color: rgba(249, 221, 211, 0.75);
    animation: expiring-pulse 2.5s ease-in-out infinite;
}
.chip-expiring em {
    color: #fb923c;
    font-weight: 600;
}

@keyframes expiring-pulse {
    0%,
    100% {
        border-color: rgba(249, 115, 22, 0.25);
    }
    50% {
        border-color: rgba(249, 115, 22, 0.6);
    }
}

/* ── 卡片格線 ────────────────────────────────────── */
.limited-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 2.5rem;
}

/* ── 卡片 ────────────────────────────────────────── */
.limited-card {
    background-color: var(--eat-surface-high);
    border-radius: 16px;
    overflow: hidden;
    border: 1px solid rgba(227, 199, 107, 0.08);
    --mx: 50%;
    --my: 50%;
    display: flex;
    flex-direction: column;
    cursor: pointer;
    transition:
        border-color 0.35s,
        transform 0.45s cubic-bezier(0.34, 1.56, 0.64, 1),
        box-shadow 0.45s ease;
    position: relative;
}
/* 售完 / 即將上架 → 禁止點擊游標 */
.limited-card.is-soldout,
.limited-card.is-upcoming {
    cursor: not-allowed;
}

/* ── 供應中卡片：脈衝金色邊框光暈（stagger by --glow-delay）── */
.limited-card:not(.is-soldout):not(.is-upcoming) {
    animation: card-glow-pulse 4.2s ease-in-out infinite;
    animation-delay: var(--glow-delay, 0s);
}
@keyframes card-glow-pulse {
    0%,
    100% {
        border-color: rgba(227, 199, 107, 0.08);
        box-shadow: none;
    }
    50% {
        border-color: rgba(227, 199, 107, 0.5);
        box-shadow:
            0 0 16px rgba(227, 199, 107, 0.15),
            0 0 36px rgba(227, 199, 107, 0.07),
            inset 0 0 20px rgba(227, 199, 107, 0.04);
    }
}

/* ── Hover：大幅拉起 + 強光暈 ── */
.limited-card::before {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: inherit;
    background: radial-gradient(
        circle at var(--mx) var(--my),
        rgba(255, 255, 255, 0.08) 0%,
        transparent 60%
    );
    pointer-events: none;
    z-index: 1;
    opacity: 0;
    transition: opacity 0.3s;
}
.limited-card:not(.is-soldout):not(.is-upcoming):hover::before {
    opacity: 1;
}
.limited-card:hover {
    border-color: rgba(227, 199, 107, 0.6) !important;
    transform: translateY(-10px);
    box-shadow:
        0 20px 60px rgba(0, 0, 0, 0.5),
        0 8px 20px rgba(227, 199, 107, 0.12);
    animation-play-state: paused;
}
.limited-card.is-soldout:hover,
.limited-card.is-upcoming:hover {
    transform: none;
    box-shadow: none;
}

/* ── 圖片掃光（hover 時從左到右掃一次）── */
.card-img-wrap::before {
    content: '';
    position: absolute;
    top: 0;
    left: -80%;
    width: 45%;
    height: 100%;
    background: linear-gradient(
        90deg,
        transparent 0%,
        rgba(255, 255, 255, 0.18) 50%,
        transparent 100%
    );
    transform: skewX(-18deg);
    z-index: 4;
    pointer-events: none;
}
.limited-card:hover .card-img-wrap::before {
    animation: img-sweep 0.65s ease forwards;
}
@keyframes img-sweep {
    from {
        left: -80%;
    }
    to {
        left: 120%;
    }
}
.limited-card.is-soldout {
    opacity: 0.7;
}
.limited-card.is-soldout::before {
    content: '';
    position: absolute;
    inset: 0;
    background: rgba(20, 10, 5, 0.45);
    border-radius: inherit;
    z-index: 4;
    pointer-events: none;
}
.limited-card.is-upcoming {
    border-color: rgba(100, 140, 220, 0.2);
}

/* ── 圖片區 ──────────────────────────────────────── */
.card-img-wrap {
    position: relative;
    height: 220px;
    overflow: hidden;
    background-color: #251813;
    flex-shrink: 0;
}
.card-img-wrap img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: transform 1s ease;
}
.limited-card:hover .card-img-wrap img {
    transform: scale(1.08);
}

.img-placeholder {
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, #362620 0%, #1e100b 100%);
}
.img-placeholder span {
    font-family: var(--font-headline);
    font-size: 5rem;
    color: rgba(227, 199, 107, 0.1);
    font-style: italic;
}

/* 售完遮罩 */
.soldout-overlay {
    position: absolute;
    inset: 0;
    background: rgba(0, 0, 0, 0.58);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 2;
}
.soldout-text {
    font-family: var(--font-label);
    font-size: 1.1rem;
    color: white;
    letter-spacing: 0.2em;
}

/* 即將上架遮罩 */
.upcoming-overlay {
    position: absolute;
    inset: 0;
    background: rgba(20, 40, 80, 0.6);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    z-index: 2;
    color: rgba(160, 200, 255, 0.9);
    font-family: var(--font-label);
    font-size: 0.9rem;
    letter-spacing: 0.15em;
}

/* 庫存條 */
.stock-bar {
    position: absolute;
    bottom: 0;
    left: 0;
    height: 3px;
    background: linear-gradient(90deg, #f97316, #fb923c 80%, rgba(251, 146, 60, 0));
    border-radius: 0 2px 0 0;
    z-index: 3;
    animation: stock-pulse 1.8s ease-in-out infinite;
}
@keyframes stock-pulse {
    0%,
    100% {
        opacity: 1;
    }
    50% {
        opacity: 0.55;
    }
}

/* 徽章群（左上） */
.badge-group {
    position: absolute;
    top: 0.85rem;
    left: 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    z-index: 3;
}
.badge {
    padding: 0.2rem 0.6rem;
    border-radius: 4px;
    font-size: 0.63rem;
    font-family: var(--font-label);
    font-weight: 600;
    letter-spacing: 0.1em;
    backdrop-filter: blur(6px);
    display: inline-block;
    transition: transform 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.badge:hover {
    transform: scale(1.2) !important;
}
.is-revealed .badge-group .badge {
    animation: badge-pop 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) both;
}
.is-revealed .badge-group .badge:nth-child(1) {
    animation-delay: 0ms;
}
.is-revealed .badge-group .badge:nth-child(2) {
    animation-delay: 60ms;
}
.is-revealed .badge-group .badge:nth-child(3) {
    animation-delay: 120ms;
}
@keyframes badge-pop {
    from {
        transform: scale(0);
        opacity: 0;
    }
    80% {
        transform: scale(1.1);
        opacity: 1;
    }
    to {
        transform: scale(1);
        opacity: 1;
    }
}
.badge-lim {
    background: rgba(227, 199, 107, 0.92);
    color: #1a0d08;
    animation: badge-sparkle 3s ease-in-out infinite;
    animation-delay: var(--glow-delay, 0s);
}
@keyframes badge-sparkle {
    0%,
    100% {
        filter: brightness(1) drop-shadow(0 0 0px rgba(227, 199, 107, 0));
    }
    50% {
        filter: brightness(1.3) drop-shadow(0 0 8px rgba(227, 199, 107, 0.8));
    }
}
.badge-rec {
    background: #e8a800;
    color: #1a0800;
    border: none;
    box-shadow: 0 1px 6px rgba(232, 168, 0, 0.45);
}
.badge-pop {
    background: rgba(217, 83, 79, 0.88);
    color: white;
}
.badge-low {
    background: rgba(200, 100, 0, 0.9);
    color: white;
}

/* 倒數徽章（右下） */
.countdown-badge {
    position: absolute;
    bottom: 0.75rem;
    right: 0.75rem;
    z-index: 3;
    background: rgba(20, 10, 5, 0.72);
    border: 1px solid rgba(227, 199, 107, 0.35);
    backdrop-filter: blur(8px);
    color: var(--eat-primary);
    font-family: 'Courier New', monospace;
    font-size: 0.72rem;
    letter-spacing: 0.05em;
    padding: 0.25rem 0.65rem;
    border-radius: 30px;
    white-space: nowrap;
    transition: border-color 0.3s;
}
.countdown-badge.urgent {
    border-color: rgba(249, 115, 22, 0.7);
    color: #fb923c;
    animation: urgent-pulse 1.5s ease-in-out infinite;
}
@keyframes urgent-pulse {
    0%,
    100% {
        box-shadow: 0 0 0 0 rgba(249, 115, 22, 0);
    }
    50% {
        box-shadow: 0 0 0 4px rgba(249, 115, 22, 0.2);
    }
}

/* ── 資訊區 ──────────────────────────────────────── */
.card-info {
    padding: 1.4rem 1.5rem 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
    flex-grow: 1;
}

.title-row {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 0.5rem;
}
.card-name {
    font-family: var(--font-headline);
    font-size: 1.2rem;
    color: var(--eat-primary);
    font-style: italic;
    margin: 0;
    line-height: 1.3;
    position: relative;
    display: inline-block;
}
.card-name::after {
    content: '';
    position: absolute;
    bottom: -2px;
    left: 0;
    width: 0;
    height: 1.5px;
    background: var(--eat-primary);
    transition: width 0.35s ease;
}
.limited-card:hover .card-name::after {
    width: 100%;
}
.card-price {
    font-family: var(--font-label);
    font-size: 0.88rem;
    color: var(--eat-secondary);
    white-space: nowrap;
    padding-top: 0.2rem;
    position: relative;
    padding: 0.1rem 0.4rem;
    border-radius: 4px;
    border: 1px solid transparent;
    transition:
        background 0.3s ease,
        border-color 0.3s ease,
        color 0.3s ease;
}
.limited-card:hover .card-price {
    background: rgba(227, 199, 107, 0.15);
    border-color: rgba(227, 199, 107, 0.25);
    color: var(--eat-primary);
}

.card-desc {
    font-family: var(--font-body);
    font-size: 0.88rem;
    line-height: 1.7;
    color: rgba(249, 221, 211, 0.55);
    font-style: italic;
    margin: 0;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

/* 供應期間 */
.period-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    border-top: 1px solid rgba(227, 199, 107, 0.08);
    padding-top: 0.75rem;
    margin-top: 0.2rem;
}
.period-icon {
    width: 14px;
    height: 14px;
    color: rgba(227, 199, 107, 0.5);
    flex-shrink: 0;
}
.period-text {
    font-family: var(--font-label);
    font-size: 0.72rem;
    color: rgba(249, 221, 211, 0.45);
    letter-spacing: 0.05em;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.period-sep {
    margin: 0 0.3em;
    opacity: 0.5;
}

/* 標籤 */
.card-tags {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
    margin-top: 0.1rem;
}
.tag {
    font-size: 0.72rem;
    padding: 0.15rem 0.6rem;
    border-radius: 20px;
    font-family: var(--font-label);
}
.tag-veg {
    background: rgba(80, 160, 80, 0.15);
    border: 1px solid rgba(80, 160, 80, 0.35);
    color: rgba(144, 238, 144, 0.8);
}
.tag-spicy {
    background: rgba(217, 83, 79, 0.12);
    border: 1px solid rgba(217, 83, 79, 0.3);
    color: rgba(255, 140, 100, 0.85);
}
.tag-take {
    background: rgba(227, 199, 107, 0.1);
    border: 1px solid rgba(227, 199, 107, 0.25);
    color: rgba(227, 199, 107, 0.7);
}

/* ── 收藏按鈕（圖片右上角，同 Menu.vue）── */
.fav-btn {
    position: absolute;
    top: 0.75rem;
    right: 0.75rem;
    z-index: 5;
    background: transparent;
    border: none;
    cursor: pointer;
    font-size: 1.2rem;
    padding: 0;
    line-height: 1;
    filter: drop-shadow(0 1px 3px rgba(0, 0, 0, 0.6));
    transition: transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.fav-btn:hover {
    transform: scale(1.25);
}

/* ── 快速預覽按鈕 ── */
.preview-btn {
    position: absolute;
    bottom: 0.65rem;
    right: 0.65rem;
    z-index: 4;
    width: 30px;
    height: 30px;
    border-radius: 50%;
    background: rgba(0, 0, 0, 0.55);
    border: 1px solid rgba(227, 199, 107, 0.3);
    color: rgba(255, 255, 255, 0.85);
    font-size: 0.85rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    opacity: 0;
    transform: scale(0.8);
    transition:
        opacity 0.22s ease,
        transform 0.22s ease,
        background 0.2s;
}
.limited-card:hover .preview-btn {
    opacity: 1;
    transform: scale(1);
}
.preview-btn:hover {
    background: rgba(0, 0, 0, 0.85);
    border-color: var(--eat-primary);
}

/* ── 快速預覽 Tooltip ── */
.preview-tip {
    position: absolute;
    top: 0.75rem;
    left: 0.75rem;
    right: 0.75rem;
    background: rgba(12, 5, 2, 0.95);
    backdrop-filter: blur(16px);
    border: 1px solid rgba(227, 199, 107, 0.25);
    border-radius: 14px;
    padding: 1rem;
    z-index: 10;
    box-shadow: 0 16px 48px rgba(0, 0, 0, 0.6);
}
.tip-desc {
    font-family: var(--font-body);
    font-size: 0.82rem;
    color: rgba(249, 221, 211, 0.75);
    font-style: italic;
    line-height: 1.6;
    margin: 0 0 0.65rem;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}
.tip-ingredients {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
    margin-bottom: 0.75rem;
}
.tip-ing-chip {
    font-family: var(--font-label);
    font-size: 0.68rem;
    padding: 0.15rem 0.55rem;
    background: rgba(227, 199, 107, 0.08);
    border: 1px solid rgba(227, 199, 107, 0.2);
    border-radius: 20px;
    color: var(--eat-secondary);
}
.tip-more {
    display: block;
    width: 100%;
    padding: 0.6rem 0 0;
    background: none;
    border: none;
    border-top: 1px solid rgba(227, 199, 107, 0.12);
    color: var(--eat-primary);
    font-family: var(--font-label);
    font-size: 0.75rem;
    letter-spacing: 0.08em;
    cursor: pointer;
    text-align: right;
    transition: opacity 0.15s;
    margin-top: 0.1rem;
}
.tip-more:hover {
    opacity: 0.75;
}
.preview-tip-enter-active {
    transition:
        opacity 0.18s ease,
        transform 0.18s cubic-bezier(0.22, 1, 0.36, 1);
}
.preview-tip-leave-active {
    transition:
        opacity 0.12s ease,
        transform 0.12s ease;
}
.preview-tip-enter-from {
    opacity: 0;
    transform: translateY(6px) scale(0.97);
}
.preview-tip-leave-to {
    opacity: 0;
    transform: translateY(3px) scale(0.98);
}
@media (max-width: 768px) {
    .preview-btn {
        display: none;
    }
}

/* ── Modal 動畫 ── */
.modal-enter-active {
    transition: opacity 0.3s ease;
}
.modal-leave-active {
    transition: opacity 0.22s ease;
}
.modal-enter-from,
.modal-leave-to {
    opacity: 0;
}

.modal-overlay {
    position: fixed;
    inset: 0;
    z-index: 1000;
    background: rgba(0, 0, 0, 0.78);
    backdrop-filter: blur(18px);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1.5rem;
}

.modal-box {
    width: 100%;
    max-width: 500px;
    background: #1a0e09;
    border: 1px solid rgba(227, 199, 107, 0.18);
    border-radius: 24px;
    overflow: hidden;
    animation: modal-pop 0.42s cubic-bezier(0.34, 1.56, 0.64, 1);
    max-height: 90vh;
    display: flex;
    flex-direction: column;
}
@keyframes modal-pop {
    from {
        opacity: 0;
        transform: scale(0.9) translateY(24px);
    }
    to {
        opacity: 1;
        transform: scale(1) translateY(0);
    }
}

/* Modal 圖片 */
.modal-img-wrap {
    position: relative;
    height: 230px;
    background: #251813;
    flex-shrink: 0;
    overflow: hidden;
}
.modal-img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}
.modal-img-placeholder {
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, #362620, #1e100b);
    font-family: var(--font-headline);
    font-size: 6rem;
    color: rgba(227, 199, 107, 0.1);
    font-style: italic;
}
.modal-img-gradient {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 55%;
    background: linear-gradient(to top, #1a0e09, transparent);
}
.modal-close {
    position: absolute;
    top: 0.85rem;
    right: 0.85rem;
    width: 30px;
    height: 30px;
    border-radius: 50%;
    background: rgba(0, 0, 0, 0.55);
    border: none;
    color: rgba(255, 255, 255, 0.75);
    font-size: 0.82rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: background 0.2s;
    z-index: 2;
}
.modal-close:hover {
    background: rgba(0, 0, 0, 0.85);
}
.share-wrap {
    position: absolute;
    top: 0.85rem;
    right: 3.35rem;
    z-index: 2;
}
.modal-share {
    width: 30px;
    height: 30px;
    border-radius: 50%;
    background: rgba(0, 0, 0, 0.55);
    border: none;
    color: rgba(255, 255, 255, 0.75);
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition:
        background 0.2s,
        color 0.2s;
}
.modal-share:hover {
    background: rgba(0, 0, 0, 0.85);
    color: var(--eat-primary);
}
.share-menu {
    position: absolute;
    top: calc(100% + 0.45rem);
    right: 0;
    background: rgba(18, 8, 4, 0.96);
    backdrop-filter: blur(16px);
    border: 1px solid rgba(227, 199, 107, 0.22);
    border-radius: 12px;
    padding: 0.35rem;
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    min-width: 148px;
    box-shadow: 0 12px 40px rgba(0, 0, 0, 0.55);
}
.share-item {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    padding: 0.48rem 0.7rem;
    border: none;
    background: none;
    border-radius: 8px;
    color: rgba(249, 221, 211, 0.82);
    font-family: var(--font-label);
    font-size: 0.78rem;
    letter-spacing: 0.04em;
    cursor: pointer;
    transition: background 0.15s;
    white-space: nowrap;
    width: 100%;
    text-align: left;
}
.share-item:hover {
    background: rgba(255, 255, 255, 0.06);
}
.share-icon {
    width: 22px;
    height: 22px;
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.72rem;
    font-weight: 700;
    flex-shrink: 0;
    line-height: 1;
}
.si-line {
    background: #06c755;
    color: white;
    border-radius: 6px;
    font-size: 0.62rem;
}
.si-fb {
    background: #1877f2;
    color: white;
    border-radius: 50%;
    font-size: 0.88rem;
}
.si-x {
    background: #0f0f0f;
    color: white;
    border-radius: 50%;
    border: 1px solid rgba(255, 255, 255, 0.2);
    font-size: 0.75rem;
}
.si-copy {
    background: rgba(227, 199, 107, 0.12);
    color: var(--eat-primary);
    border: 1px solid rgba(227, 199, 107, 0.3);
    border-radius: 6px;
}
.share-menu-enter-active {
    transition:
        opacity 0.18s ease,
        transform 0.18s cubic-bezier(0.22, 1, 0.36, 1);
}
.share-menu-leave-active {
    transition:
        opacity 0.12s ease,
        transform 0.12s ease;
}
.share-menu-enter-from {
    opacity: 0;
    transform: scale(0.88) translateY(-8px);
    transform-origin: top right;
}
.share-menu-leave-to {
    opacity: 0;
    transform: scale(0.92) translateY(-4px);
    transform-origin: top right;
}
.modal-badge-group {
    position: absolute;
    bottom: 0.9rem;
    left: 1rem;
    display: flex;
    gap: 0.4rem;
    z-index: 2;
}

/* Modal Body */
.modal-body {
    padding: 1.5rem 1.75rem 1.75rem;
    overflow-y: auto;
    scrollbar-width: thin;
    scrollbar-color: rgba(227, 199, 107, 0.2) transparent;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.modal-title-row {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 0.75rem;
}
.modal-title {
    font-family: var(--font-headline);
    font-size: 1.7rem;
    color: var(--eat-primary);
    font-style: italic;
    margin: 0;
    line-height: 1.2;
}
.modal-price {
    font-family: var(--font-label);
    font-size: 1rem;
    color: var(--eat-secondary);
    white-space: nowrap;
    padding-top: 0.3rem;
}

/* Modal 收藏按鈕 */
.modal-fav-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    background: none;
    border: 1px solid rgba(227, 199, 107, 0.2);
    border-radius: 30px;
    color: rgba(249, 221, 211, 0.6);
    font-family: var(--font-label);
    font-size: 0.75rem;
    letter-spacing: 0.1em;
    padding: 0.35rem 1rem;
    cursor: pointer;
    transition:
        border-color 0.2s,
        color 0.2s,
        background 0.2s;
    align-self: flex-start;
}
.modal-fav-btn:hover {
    border-color: rgba(227, 199, 107, 0.5);
    color: var(--eat-primary);
    background: rgba(227, 199, 107, 0.06);
}

/* Modal 提醒按鈕 */
.modal-remind-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    background: none;
    border: 1px solid rgba(126, 207, 126, 0.25);
    border-radius: 30px;
    color: rgba(144, 238, 144, 0.65);
    font-family: var(--font-label);
    font-size: 0.75rem;
    letter-spacing: 0.1em;
    padding: 0.35rem 1rem;
    cursor: pointer;
    transition:
        border-color 0.2s,
        color 0.2s,
        background 0.2s;
    align-self: flex-start;
}
.modal-remind-btn:hover:not(:disabled) {
    border-color: rgba(126, 207, 126, 0.55);
    color: #7ecf7e;
    background: rgba(126, 207, 126, 0.07);
}
.modal-remind-btn.is-set,
.modal-remind-btn:disabled {
    border-color: rgba(126, 207, 126, 0.45);
    color: #7ecf7e;
    background: rgba(126, 207, 126, 0.08);
    cursor: default;
    opacity: 0.85;
}

.modal-rate-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    background: none;
    border: 1px solid rgba(227, 199, 107, 0.25);
    border-radius: 30px;
    color: rgba(227, 199, 107, 0.7);
    font-family: var(--font-label);
    font-size: 0.75rem;
    letter-spacing: 0.1em;
    padding: 0.35rem 1rem;
    cursor: pointer;
    transition:
        border-color 0.2s,
        color 0.2s,
        background 0.2s;
    align-self: flex-start;
}
.modal-rate-btn:hover {
    border-color: var(--eat-primary);
    color: var(--eat-primary);
    background: rgba(227, 199, 107, 0.07);
}

.modal-desc {
    font-family: var(--font-body);
    font-size: 0.9rem;
    line-height: 1.75;
    color: rgba(249, 221, 211, 0.6);
    font-style: italic;
    margin: 0;
}

/* 倒數區塊 */
.modal-countdown {
    background: rgba(227, 199, 107, 0.06);
    border: 1px solid rgba(227, 199, 107, 0.2);
    border-radius: 12px;
    padding: 0.85rem 1.25rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
}
.modal-countdown.urgent {
    border-color: rgba(249, 115, 22, 0.4);
    background: rgba(249, 115, 22, 0.06);
    animation: urgent-pulse 1.5s ease-in-out infinite;
}
.modal-countdown-label {
    font-family: var(--font-label);
    font-size: 0.68rem;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: rgba(249, 221, 211, 0.4);
}
.modal-countdown-value {
    font-family: 'Courier New', monospace;
    font-size: 1.1rem;
    color: var(--eat-primary);
    letter-spacing: 0.05em;
}
.modal-countdown.urgent .modal-countdown-value {
    color: #fb923c;
}

/* 供應期間 */
.modal-period {
    display: flex;
    align-items: center;
    gap: 0.85rem;
    border-top: 1px solid rgba(227, 199, 107, 0.08);
    padding-top: 0.85rem;
}
.modal-period-icon {
    width: 18px;
    height: 18px;
    color: rgba(227, 199, 107, 0.45);
    flex-shrink: 0;
}
.modal-period-label {
    font-family: var(--font-label);
    font-size: 0.62rem;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: rgba(249, 221, 211, 0.35);
    margin-bottom: 0.2rem;
}
.modal-period-dates {
    font-family: var(--font-label);
    font-size: 0.82rem;
    color: rgba(249, 221, 211, 0.65);
    letter-spacing: 0.05em;
}

/* 屬性 Chips */
.modal-attrs {
    display: flex;
    flex-wrap: wrap;
    gap: 0.45rem;
}
.attr-chip {
    font-size: 0.78rem;
    padding: 0.28rem 0.85rem;
    border-radius: 20px;
    border: 1px solid rgba(227, 199, 107, 0.18);
    color: rgba(249, 221, 211, 0.6);
    font-family: var(--font-label);
}
.attr-chip.veg {
    border-color: rgba(80, 160, 80, 0.35);
    color: rgba(144, 238, 144, 0.8);
}
.attr-chip.spicy {
    border-color: rgba(217, 83, 79, 0.35);
    color: rgba(255, 140, 100, 0.85);
}
.attr-chip.take {
    border-color: rgba(227, 199, 107, 0.25);
    color: rgba(227, 199, 107, 0.7);
}
.attr-chip.rec {
    border-color: rgba(227, 199, 107, 0.35);
    color: var(--eat-primary);
}
.attr-chip.pop {
    border-color: rgba(217, 83, 79, 0.3);
    color: rgba(255, 140, 100, 0.8);
}

/* ── 進場動畫 ────────────────────────────────────── */
.card-enter-active {
    transition:
        opacity 0.5s ease,
        transform 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.card-leave-active {
    transition:
        opacity 0.3s ease,
        transform 0.3s ease;
}
.card-enter-from {
    opacity: 0;
    transform: translateY(24px) scale(0.97);
}
.card-leave-to {
    opacity: 0;
    transform: translateY(-8px) scale(0.97);
}
.card-move {
    transition: transform 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
}

/* ── States ──────────────────────────────────────── */
.state-container {
    padding: 8rem 0;
    text-align: center;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
}
.spinner {
    width: 48px;
    height: 48px;
    border: 2px solid rgba(227, 199, 107, 0.1);
    border-top-color: var(--eat-primary);
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 1rem;
}
@keyframes spin {
    to {
        transform: rotate(360deg);
    }
}
.loading-text {
    font-family: var(--font-label);
    letter-spacing: 0.2em;
    color: var(--eat-secondary);
    font-size: 0.9rem;
}
.error-msg {
    font-family: var(--font-body);
    color: rgba(249, 221, 211, 0.55);
    font-style: italic;
    margin: 0;
}
.retry-btn {
    background: none;
    border: 1px solid var(--eat-primary);
    color: var(--eat-primary);
    padding: 0.55rem 2.25rem;
    font-family: var(--font-label);
    font-size: 0.78rem;
    cursor: pointer;
    border-radius: 30px;
    letter-spacing: 0.2em;
    text-transform: uppercase;
    transition: background 0.3s;
}
.retry-btn:hover {
    background: rgba(227, 199, 107, 0.1);
}

/* Empty State */
.empty-state {
    gap: 0.6rem;
}
.empty-icon {
    width: 72px;
    height: 90px;
    color: var(--eat-primary);
    opacity: 0.5;
    margin-bottom: 0.75rem;
}
.empty-title {
    font-family: var(--font-headline);
    font-size: 1.2rem;
    color: var(--eat-primary);
    font-style: italic;
    margin: 0;
}
.empty-sub {
    font-family: var(--font-body);
    font-size: 0.88rem;
    color: rgba(249, 221, 211, 0.38);
    font-style: italic;
    margin: 0;
}

/* ── Utils ───────────────────────────────────────── */
.container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 2rem;
}
.py-main {
    padding-top: 1.5rem;
    padding-bottom: 6rem;
}

/* ── DEV 專用：懸浮發信按鈕 ─────────────────────── */
.dev-remind-btn {
    position: fixed;
    bottom: 2rem;
    right: 2rem;
    z-index: 900;
    display: inline-flex;
    align-items: center;
    gap: 0.55rem;
    padding: 0.6rem 1.25rem;
    background: rgba(12, 5, 2, 0.88);
    border: 1px solid rgba(227, 199, 107, 0.45);
    border-radius: 30px;
    color: var(--eat-primary);
    font-family: var(--font-label);
    font-size: 0.78rem;
    letter-spacing: 0.1em;
    cursor: pointer;
    backdrop-filter: blur(12px);
    box-shadow: 0 4px 24px rgba(0, 0, 0, 0.5);
    transition:
        border-color 0.2s,
        background 0.2s,
        opacity 0.2s;
}
.dev-remind-btn:hover:not(:disabled) {
    border-color: rgba(227, 199, 107, 0.8);
    background: rgba(227, 199, 107, 0.08);
}
.dev-remind-btn:disabled {
    opacity: 0.55;
    cursor: not-allowed;
}
.dev-label {
    font-size: 0.6rem;
    color: rgba(249, 221, 211, 0.4);
    border: 1px solid rgba(249, 221, 211, 0.2);
    border-radius: 4px;
    padding: 0.08rem 0.4rem;
    letter-spacing: 0.05em;
}

/* ── RWD ─────────────────────────────────────────── */
@media (max-width: 768px) {
    .limited-header {
        padding: 6rem 0 1.5rem;
    }
    .limited-grid {
        grid-template-columns: 1fr;
    }
    .countdown-badge {
        font-size: 0.65rem;
    }
}
</style>
