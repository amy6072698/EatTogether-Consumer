<template>
    <div class="menu-page">
        <header class="menu-header">
            <div class="menu-header-bg" ref="headerBgRef"></div>
            <div class="menu-header-overlay"></div>
            <div class="container" style="position: relative; z-index: 2">
                <span class="menu-eyebrow">Signature Flavors</span>
                <h1 class="eat-h1">精選菜單</h1>

                <nav class="category-tabs">
                    <button
                        v-for="cat in categories"
                        :key="cat.id"
                        class="tab-btn"
                        :class="{ active: currentCategory === cat.id }"
                        @click="currentCategory = cat.id"
                    >
                        {{ cat.name }}
                    </button>
                </nav>
            </div>
        </header>

        <main class="container py-5">
            <!-- 搜尋 + 過濾列 -->
            <div class="filter-bar">
                <div class="search-wrap">
                    <span class="search-icon">🔍</span>
                    <input
                        v-model="searchQuery"
                        type="text"
                        placeholder="搜尋餐點名稱或描述..."
                        class="search-input"
                    />
                    <button v-if="searchQuery" class="search-clear" @click="searchQuery = ''">
                        ✕
                    </button>
                </div>
                <div class="filter-chips">
                    <button
                        class="filter-chip"
                        :class="{ active: filterPopular }"
                        @click="filterPopular = !filterPopular"
                        @mousedown="ripple"
                    >
                        👑 人氣餐點
                    </button>
                    <button
                        class="filter-chip"
                        :class="{ active: filterRec }"
                        @click="filterRec = !filterRec"
                        @mousedown="ripple"
                    >
                        ⭐ 主廚推薦
                    </button>
                    <button
                        class="filter-chip"
                        :class="{ active: filterSpicy }"
                        @click="filterSpicy = !filterSpicy"
                        @mousedown="ripple"
                    >
                        🌶️ 有辣
                    </button>
                    <button
                        class="filter-chip"
                        :class="{ active: filterVeg }"
                        @click="filterVeg = !filterVeg"
                        @mousedown="ripple"
                    >
                        🥬 素食
                    </button>
                    <button
                        class="filter-chip"
                        :class="{ active: filterFav }"
                        @click="filterFav = !filterFav"
                        @mousedown="ripple"
                    >
                        ❤️ 我的最愛
                    </button>
                </div>
                <div class="view-toggle">
                    <button
                        class="view-btn"
                        :class="{ active: viewMode === 'grid' }"
                        @click="viewMode = 'grid'"
                        title="卡片模式"
                    >
                        ⊞
                    </button>
                    <button
                        class="view-btn"
                        :class="{ active: viewMode === 'list' }"
                        @click="viewMode = 'list'"
                        title="列表模式"
                    >
                        ≡
                    </button>
                </div>
                <select v-model="sortOrder" class="sort-select">
                    <option value="default">預設排序</option>
                    <option value="price-asc">價格低到高</option>
                    <option value="price-desc">價格高到低</option>
                    <option value="recommended">推薦優先</option>
                </select>
            </div>

            <!-- 篩選統計 -->
            <p class="filter-stats">
                <span v-if="!hasActiveFilter">共 {{ filteredDishes.length }} 道餐點</span>
                <span v-else>找到 {{ filteredDishes.length }} 道符合條件的餐點</span>
            </p>

            <!-- Loading -->
            <div v-if="loading" class="state-container">
                <div class="spinner"></div>
                <p class="loading-text">正在為您準備美味佳餚...</p>
            </div>

            <!-- Error -->
            <div v-else-if="error" class="state-container error-state">
                <div class="error-icon">!</div>
                <p>{{ error }}</p>
                <button @click="fetchMenu" class="retry-btn">重新嘗試</button>
            </div>

            <!-- 餐點卡片 -->
            <TransitionGroup
                v-else
                name="card"
                tag="div"
                :class="viewMode === 'grid' ? 'menu-grid' : 'menu-list'"
            >
                <div
                    v-for="dish in filteredDishes"
                    :key="dish.id"
                    class="dish-card"
                    :class="{ 'is-soldout': dish.stockStatus === 2 }"
                    @click="dish.stockStatus !== 2 && openModal(dish)"
                    @mousemove="handleMouseMove($event)"
                >
                    <div class="dish-img-wrap">
                        <img
                            v-if="dish.imageUrl"
                            :src="formatImageUrl(dish.imageUrl)"
                            :alt="dish.dishName"
                            loading="lazy"
                        />
                        <div v-else class="img-placeholder">
                            <span>{{ dish.dishName.charAt(0) }}</span>
                        </div>
                        <div v-if="dish.stockStatus === 2" class="soldout-overlay">
                            <span class="soldout-text">售完</span>
                        </div>
                        <FavoriteButton
                            :dish-id="dish.id"
                            :is-favorited="favorites.includes(dish.id)"
                            @toggle="toggleFavorite"
                        />
                        <div class="badge-group">
                            <span v-if="dish.isRecommended" class="badge badge-rec">推薦</span>
                            <span v-if="dish.isPopular" class="badge badge-pop">熱銷</span>
                            <span v-if="dish.isVegetarian" class="badge badge-veg">素食</span>
                            <span v-if="dish.stockStatus === 1" class="badge badge-low-stock"
                                >剩餘不多</span
                            >
                            <template v-if="dish.isLimited">
                                <span
                                    v-if="getCountdown(dish.endDate, dish.startDate) === '尚未供應'"
                                    class="badge badge-pending"
                                    >尚未供應</span
                                >
                                <span
                                    v-else-if="
                                        getCountdown(dish.endDate, dish.startDate) === '即將開始'
                                    "
                                    class="badge badge-soon"
                                    >即將開始</span
                                >
                                <span
                                    v-else-if="
                                        getCountdown(dish.endDate, dish.startDate) === '已結束'
                                    "
                                    class="badge badge-sold"
                                    >停止供應</span
                                >
                                <span v-else class="badge badge-lim fomo-timer">{{
                                    getCountdown(dish.endDate, dish.startDate)
                                }}</span>
                            </template>
                        </div>
                    </div>

                    <div class="dish-info">
                        <div class="dish-title-row">
                            <h3 class="dish-name">{{ dish.dishName }}</h3>
                            <div class="dish-price-wrap">
                                <span class="dish-price"
                                    >NT$ {{ dish.price.toLocaleString() }}</span
                                >
                            </div>
                        </div>
                        <p class="dish-desc">
                            {{
                                dish.description ||
                                '精選新鮮食材，傳承義式經典風味，每一口都是主廚的心意。'
                            }}
                        </p>
                        <div class="dish-footer">
                            <div class="dish-tags">
                                <span
                                    class="tag rating-tag"
                                    v-if="
                                        (dishRatingMap[dish.id]?.ratingCount ?? dish.ratingCount) >
                                        0
                                    "
                                    >★
                                    {{
                                        (
                                            dishRatingMap[dish.id]?.averageScore ??
                                            dish.averageScore
                                        ).toFixed(1)
                                    }}</span
                                >
                                <span class="tag veg-tag" v-if="dish.isVegetarian">🥬 素食</span>
                                <span class="tag spicy-tag" v-if="dish.spicyLevel > 0">
                                    {{ '🌶️'.repeat(dish.spicyLevel) }}
                                </span>
                            </div>
                            <div class="category-name">{{ dish.categoryName }}</div>
                        </div>
                    </div>
                </div>
            </TransitionGroup>

            <!-- Empty -->
            <div
                v-if="!loading && !error && filteredDishes.length === 0"
                class="state-container empty-state"
            >
                <p>找不到符合條件的餐點，換個關鍵字或分類試試看。</p>
            </div>
        </main>

        <ToastContainer />

        <!-- ── Return to SetMeal button ── -->
        <Transition name="return-btn">
            <button v-if="returnTo" class="return-setmeal-btn" @click="router.push(returnTo)">
                {{ returnLabel }}
            </button>
        </Transition>

        <!-- ── Modal ── -->
        <Transition name="modal">
            <div v-if="isModalOpen && selectedDish" class="modal-overlay" @click.self="closeModal">
                <div class="modal-box" ref="modalBoxRef">
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
                        <div class="modal-badge-group">
                            <span v-if="selectedDish.isRecommended" class="badge badge-rec"
                                >推薦</span
                            >
                            <span v-if="selectedDish.isPopular" class="badge badge-pop">熱銷</span>
                            <span v-if="selectedDish.isVegetarian" class="badge badge-veg"
                                >素食</span
                            >
                        </div>
                    </div>

                    <!-- 浮動按鈕 (position:fixed, JS 定位) -->
                    <button
                        class="modal-close"
                        :style="{ top: btnPos.top, right: btnPos.closeRight }"
                        @click="closeModal"
                    >
                        ✕
                    </button>
                    <ShareMenu
                        v-model="shareMenuOpen"
                        share-url=""
                        share-title=""
                        :style="{
                            position: 'fixed',
                            top: btnPos.top,
                            right: btnPos.shareRight,
                            zIndex: 1100,
                        }"
                        @select="openShareItem"
                    />

                    <!-- 資訊區 -->
                    <div class="modal-body" ref="modalBodyRef">
                        <div class="modal-header-row">
                            <h2 class="modal-title">{{ selectedDish.dishName }}</h2>
                            <div class="modal-price">
                                NT$ {{ selectedDish.price.toLocaleString() }}
                            </div>
                        </div>

                        <!-- 描述 -->
                        <p class="text-on-surface-variant font-body text-sm italic mb-8">
                            {{
                                selectedDish.description ||
                                '主廚精選地道食材，為您呈現純粹的義式饗宴。'
                            }}
                        </p>

                        <!-- 精選食材 -->
                        <div
                            v-if="parseIngredients(selectedDish.ingredientsJson).length"
                            class="modal-section"
                        >
                            <h3
                                class="text-primary font-headline font-bold text-sm mb-4 tracking-widest"
                            >
                                精選食材
                                <span class="ingredient-hint">點擊食材了解詳情</span>
                            </h3>
                            <div class="grid grid-cols-2 md:grid-cols-3 gap-3 mb-4">
                                <div
                                    v-for="item in parseIngredients(selectedDish.ingredientsJson)"
                                    :key="item.name"
                                    class="bg-surface-container-low p-4 rounded-xl transition-all ingredient-card"
                                    :class="{ 'is-active': activeIngredient === item.name }"
                                    @click="fetchIngredientInfo(item.name)"
                                >
                                    <span
                                        class="text-on-surface font-headline font-bold text-xs block mb-1"
                                        >{{ item.name }}</span
                                    >
                                    <span
                                        class="text-on-surface-variant font-body text-[10px] italic"
                                        >{{ item.subDesc }}</span
                                    >
                                </div>
                            </div>
                            <!-- AI 食材資訊面板 -->
                            <Transition name="ingredient-panel">
                                <div v-if="activeIngredient" class="ingredient-info-panel mb-8">
                                    <div class="ingredient-info-title">
                                        ✦ {{ activeIngredient }}
                                    </div>
                                    <div v-if="loadingIngredient" class="ingredient-loading">
                                        <span class="ingredient-spinner"></span> 正在查詢食材資料...
                                    </div>
                                    <pre v-else class="ingredient-info-text">{{
                                        ingredientInfo
                                    }}</pre>
                                </div>
                            </Transition>
                        </div>

                        <!-- 屬性列 -->
                        <div class="modal-section">
                            <div class="modal-section-label">餐點屬性</div>
                            <div class="attr-chips">
                                <span class="attr-chip" v-if="selectedDish.categoryName">{{
                                    selectedDish.categoryName
                                }}</span>
                                <span class="attr-chip veg" v-if="selectedDish.isVegetarian"
                                    >🥬 素食</span
                                >
                                <span class="attr-chip spicy" v-if="selectedDish.spicyLevel > 0">
                                    {{ '🌶️'.repeat(selectedDish.spicyLevel) }}
                                    {{ spicyLabel(selectedDish.spicyLevel) }}
                                </span>
                                <span class="attr-chip rec" v-if="selectedDish.isRecommended"
                                    >⭐ 主廚推薦</span
                                >
                                <span class="attr-chip pop" v-if="selectedDish.isPopular"
                                    >🔥 熱銷</span
                                >
                            </div>
                        </div>

                        <!-- 評分 -->
                        <div class="modal-section rating-section">
                            <DishRatingSection
                                :dish="selectedDish"
                                :is-logged-in="authStore.isLoggedIn"
                                @rated="
                                    ({ dishId, averageScore, ratingCount }) =>
                                        (dishRatingMap[dishId] = { averageScore, ratingCount })
                                "
                                @login="openAuthModal"
                            />
                        </div>

                        <!-- 留言區 -->
                        <div class="modal-section review-section">
                            <ReviewSection
                                :dish-id="selectedDish.id"
                                :is-logged-in="authStore.isLoggedIn"
                                :member-name="authStore.member?.name ?? ''"
                                @login="openAuthModal"
                            />
                        </div>
                    </div>
                </div>
            </div>
        </Transition>
    </div>
</template>

<script setup>
import { ref, reactive, computed, watch, nextTick, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Modal } from 'bootstrap'
import ToastContainer from '@/components/common/ToastContainer.vue'
import { useToast } from '@/composables/useToast.js'
import apiFetch from '@/utils/apiFetch.js'
import { useAuthStore } from '@/stores/auth.js'
import ShareMenu from '@/components/common/menu/ShareMenu.vue'
import DishRatingSection from '@/components/common/menu/DishRatingSection.vue'
import ReviewSection from '@/components/common/menu/ReviewSection.vue'
import FavoriteButton from '@/components/common/menu/FavoriteButton.vue'

// ── 安全解析食材 JSON ────────────────────────────────
const parseIngredients = (jsonString) => {
    if (!jsonString) return []
    try {
        const result = JSON.parse(jsonString)
        return Array.isArray(result) ? result : []
    } catch {
        return []
    }
}

const { show } = useToast()
const authStore = useAuthStore()
const openAuthModal = () => Modal.getOrCreateInstance(document.querySelector('#authModal')).show()

// ── Route / Router ────────────────────────────────────
const route = useRoute()
const router = useRouter()
const returnTo = computed(() => route.query.returnTo || null)
const returnLabel = computed(() => {
    if (!returnTo.value) return ''
    if (returnTo.value.startsWith('/limited')) return '← 返回限定餐點'
    if (returnTo.value.startsWith('/setmeal')) return '← 返回套餐'
    return '← 返回'
})

// ── Share ─────────────────────────────────────────────
const shareMenuOpen = ref(false)

const openShareItem = async (type) => {
    const dish = selectedDish.value
    if (!dish) return
    const dishUrl = `${window.location.origin}/menu?dish=${dish.id}`
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

// ── Ingredient AI info ────────────────────────────────
const activeIngredient = ref(null)
const ingredientInfo = ref('')
const loadingIngredient = ref(false)

const fetchIngredientInfo = async (name) => {
    if (activeIngredient.value === name) {
        activeIngredient.value = null
        return
    }
    activeIngredient.value = name
    ingredientInfo.value = ''
    loadingIngredient.value = true
    try {
        const res = await apiFetch('/Ingredients/info', {
            method: 'POST',
            body: JSON.stringify({ ingredientName: name }),
        })
        if (!res.ok) throw new Error()
        const data = await res.json()
        ingredientInfo.value = data.text || ''
    } catch {
        ingredientInfo.value = '無法取得食材資訊，請稍後再試。'
    } finally {
        loadingIngredient.value = false
    }
}

// ── Card mouse tracking (reflection + glare, no tilt) ─
const handleMouseMove = (event) => {
    const card = event.currentTarget
    const rect = card.getBoundingClientRect()
    const relX = (event.clientX - rect.left) / rect.width
    const relY = (event.clientY - rect.top) / rect.height
    card.style.setProperty('--glare-x', `${(relX * 100).toFixed(1)}%`)
    card.style.setProperty('--glare-y', `${(relY * 100).toFixed(1)}%`)
    card.style.setProperty('--mx', `${(relX * 100).toFixed(1)}%`)
    card.style.setProperty('--my', `${(relY * 100).toFixed(1)}%`)
}

// ── 倒數計時 ─────────────────────────────────────────
const currentTime = ref(new Date())
let _clockTimer = null
onMounted(() => {
    _clockTimer = setInterval(() => {
        currentTime.value = new Date()
    }, 1000)
})
onUnmounted(() => {
    clearInterval(_clockTimer)
})

// 回傳四種狀態：'尚未供應' | '即將開始' | 倒數字串 | '已結束'
const getCountdown = (endDateString, startDateString) => {
    if (!endDateString) return ''
    const now = currentTime.value // 存取 .value 讓 Vue 追蹤響應性
    const end = new Date(endDateString)
    const start = startDateString ? new Date(startDateString) : null

    // 已超過結束時間
    if (end <= now) return '已結束'

    // 尚未開始
    if (start && start > now) {
        const msToStart = start - now
        // 超過 24 小時才開始 → 尚未供應；24 小時內 → 即將開始
        return msToStart > 24 * 60 * 60 * 1000 ? '尚未供應' : '即將開始'
    }

    // 供應中 — 計算距離結束的倒數
    const totalSec = Math.floor((end - now) / 1000)
    const days = Math.floor(totalSec / 86400)
    const hours = Math.floor((totalSec % 86400) / 3600)
    const minutes = Math.floor((totalSec % 3600) / 60)
    const seconds = totalSec % 60

    if (days >= 1) return `剩餘 ${days} 天 ${hours} 小時`
    const hh = String(hours).padStart(2, '0')
    const mm = String(minutes).padStart(2, '0')
    const ss = String(seconds).padStart(2, '0')
    return `倒數 ${hh}:${mm}:${ss}`
}

// ── Polling ──────────────────────────────────────────
let _pollTimer = null
let _dishFingerprint = ''

const getDishFingerprint = (data) =>
    [...data]
        .sort((a, b) => a.id - b.id)
        .map((d) => `${d.id}|${d.dishName}|${d.price}|${d.stockStatus}|${d.isActive ?? 1}`)
        .join(',')

const pollMenu = async () => {
    try {
        const res = await apiFetch('/Dishes/active')
        if (!res.ok) return
        const newData = await res.json()
        const newFp = getDishFingerprint(newData)
        if (newFp !== _dishFingerprint) {
            _dishFingerprint = newFp
            dishes.value = newData
        }
    } catch (e) {
        console.warn('[pollMenu]', e)
    }
}

// ── State ────────────────────────────────────────────
const dishes = ref([])
const loading = ref(true)
const error = ref(null)
const currentCategory = ref(0)
const searchQuery = ref('')
const filterVeg = ref(false)
const filterSpicy = ref(false)
const filterRec = ref(false)
const filterFav = ref(false)
const filterPopular = ref(false)
const sortOrder = ref('default')
const viewMode = ref('grid')

// ── 評分（快取，供 @rated 事件更新及 openModal 載入） ──
const dishRatingMap = reactive({})

// ── 收藏 ──────────────────────────────────────────────
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

watch(
    () => authStore.isLoggedIn,
    async (loggedIn) => {
        if (loggedIn) {
            await loadFavorites()
        } else {
            favorites.value = []
        }
    }
)

// ── Filter chip ripple ────────────────────────────────
const ripple = (e) => {
    const btn = e.currentTarget
    const circle = document.createElement('span')
    const rect = btn.getBoundingClientRect()
    circle.className = 'ripple-wave'
    circle.style.left = `${e.clientX - rect.left}px`
    circle.style.top = `${e.clientY - rect.top}px`
    btn.appendChild(circle)
    circle.addEventListener('animationend', () => circle.remove())
}

// ── Header parallax ───────────────────────────────────
const headerBgRef = ref(null)
const handleParallax = () => {
    if (!headerBgRef.value) return
    headerBgRef.value.style.transform = `translateY(${window.scrollY * 0.4}px)`
}

// Modal
const isModalOpen = ref(false)
const selectedDish = ref(null)
const modalBoxRef = ref(null)
const modalImgRef = ref(null)
const modalBodyRef = ref(null)

const btnPos = reactive({ top: '1rem', closeRight: '1rem', shareRight: '3.5rem' })
const updateBtnPos = () => {
    if (!modalBoxRef.value) return
    const r = modalBoxRef.value.getBoundingClientRect()
    btnPos.top = `${r.top + 16}px`
    btnPos.closeRight = `${window.innerWidth - r.right + 16}px`
    btnPos.shareRight = `${window.innerWidth - r.right + 56}px`
}

watch(isModalOpen, async (open) => {
    if (!open) return
    await nextTick()
    updateBtnPos()

    // 淡入
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

    // Chips 預先隱藏
    const chips = body.querySelectorAll('.attr-chip')
    chips.forEach((chip) => {
        chip.style.transition = 'none'
        chip.style.opacity = '0'
        chip.style.transform = 'scale(0) translateY(6px)'
    })

    // Body sections 預先隱藏
    sections.forEach((el) => {
        el.style.transition = 'none'
        el.style.opacity = '0'
        el.style.transform = 'translateY(30px) scale(0.96)'
    })

    body.offsetHeight

    // Body sections 依序淡入
    sections.forEach((el, i) => {
        setTimeout(() => {
            el.style.transition =
                'opacity 0.55s cubic-bezier(0.22, 1, 0.36, 1), transform 0.55s cubic-bezier(0.22, 1, 0.36, 1)'
            el.style.opacity = '1'
            el.style.transform = 'translateY(0) scale(1)'
        }, i * 90)
    })

    // Chips 彈跳進場
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

const categories = [
    { id: 0, name: '全部' },
    { id: 1, name: '主餐' },
    { id: 2, name: '飲料' },
    { id: 3, name: '甜點' },
    { id: 4, name: '湯品' },
    { id: 5, name: '附餐' },
]

// ── Modal ────────────────────────────────────────────
const openModal = async (dish) => {
    selectedDish.value = dish
    isModalOpen.value = true
    document.body.style.overflow = 'hidden'

    try {
        const res = await apiFetch(`/Dishes/${dish.id}/Rating`)
        if (res.ok) {
            const data = await res.json()
            dishRatingMap[dish.id] = data
        }
    } catch {
        /* 忽略評分載入錯誤 */
    }
}

const closeModal = () => {
    isModalOpen.value = false
    selectedDish.value = null
    shareMenuOpen.value = false
    activeIngredient.value = null
    document.body.style.overflow = ''
}

const handleEsc = (e) => {
    if (e.key === 'Escape') closeModal()
}
onMounted(() => window.addEventListener('keydown', handleEsc))
onUnmounted(() => window.removeEventListener('keydown', handleEsc))

// ── Utils ────────────────────────────────────────────
const spicyLabel = (level) => {
    return ['', '微辣', '中辣', '大辣', '極辣'][level] ?? '辣'
}

const formatImageUrl = (url) => {
    if (!url) return null
    return url
}

// ── API ──────────────────────────────────────────────
const fetchMenu = async () => {
    loading.value = true
    error.value = null
    try {
        const res = await apiFetch('/Dishes/active')
        if (!res.ok) throw new Error(`抓取失敗 (${res.status})`)
        dishes.value = await res.json()
    } catch (err) {
        console.error('Menu Fetch Error:', err)
        error.value = '無法載入菜單資料，請確認 API 伺服器狀態。'
    } finally {
        loading.value = false
    }
}

// ── Computed ─────────────────────────────────────────
const hasActiveFilter = computed(
    () =>
        searchQuery.value !== '' ||
        filterVeg.value ||
        filterSpicy.value ||
        filterRec.value ||
        filterFav.value ||
        filterPopular.value ||
        currentCategory.value !== 0 ||
        sortOrder.value !== 'default'
)
const filteredDishes = computed(() => {
    const result = dishes.value.filter((d) => {
        if (currentCategory.value !== 0 && d.categoryId !== currentCategory.value) return false
        if (searchQuery.value) {
            const q = searchQuery.value.toLowerCase()
            const hit =
                d.dishName.toLowerCase().includes(q) ||
                (d.description && d.description.toLowerCase().includes(q))
            if (!hit) return false
        }
        if (filterVeg.value && !d.isVegetarian) return false
        if (filterSpicy.value && !(d.spicyLevel > 0)) return false
        if (filterRec.value && !d.isRecommended) return false
        if (filterFav.value && !favorites.value.includes(d.id)) return false
        if (filterPopular.value && !d.isPopular) return false
        return true
    })

    switch (sortOrder.value) {
        case 'price-asc':
            return [...result].sort((a, b) => a.price - b.price)
        case 'price-desc':
            return [...result].sort((a, b) => b.price - a.price)
        case 'recommended':
            return [...result].sort((a, b) => (b.isRecommended ? 1 : 0) - (a.isRecommended ? 1 : 0))
        default:
            return [...result].sort(
                (a, b) => (a.stockStatus === 2 ? 1 : 0) - (b.stockStatus === 2 ? 1 : 0)
            )
    }
})

onMounted(async () => {
    await Promise.all([fetchMenu(), loadFavorites()])
    _dishFingerprint = getDishFingerprint(dishes.value)
    _pollTimer = setInterval(pollMenu, 3000)
    window.addEventListener('scroll', handleParallax, { passive: true })
    window.addEventListener('resize', updateBtnPos)

    // 深層連結：?dish=id 自動開 Modal
    const dishParam = route.query.dish
    if (dishParam) {
        const dish = dishes.value.find((d) => String(d.id) === dishParam)
        if (dish) openModal(dish)
    }
})
onUnmounted(() => {
    clearInterval(_pollTimer)
    window.removeEventListener('scroll', handleParallax)
    window.removeEventListener('resize', updateBtnPos)
    document.body.style.overflow = ''
})
</script>

<style scoped>
.menu-page {
    background-color: var(--eat-surface);
    min-height: 100vh;
    color: var(--eat-on-surface);
    font-family: var(--font-body);
}

.menu-header {
    padding: 6rem 0 2.5rem;
    text-align: center;
    position: relative;
    overflow: hidden;
}
.menu-header .container {
    display: flex;
    flex-direction: column;
    min-height: 12rem;
    padding-bottom: 0;
}
.menu-header .eat-h1 {
    margin-bottom: 0;
}
.menu-header-bg {
    position: absolute;
    inset: 0;
    background-image: url('https://images.unsplash.com/photo-1476224203421-9ac39bcb3327?w=1400&q=80');
    background-size: cover;
    background-position: center 40%;
    will-change: transform;
}
.menu-header-overlay {
    position: absolute;
    inset: 0;
    background: linear-gradient(
        to bottom,
        rgba(24, 11, 6, 0.8) 0%,
        rgba(24, 11, 6, 0.7) 55%,
        var(--eat-surface) 100%
    );
}

.menu-eyebrow {
    display: block;
    font-family: var(--font-label);
    color: var(--eat-secondary);
    letter-spacing: 0.4em;
    text-transform: uppercase;
    font-size: 0.75rem;
    margin-bottom: 1rem;
}

.eat-h1 {
    font-family: var(--font-headline);
    font-size: clamp(2.5rem, 5vw, 3.5rem);
    color: var(--eat-primary);
    margin-bottom: 3rem;
    font-style: italic;
}

/* Category Tabs */
.category-tabs {
    display: flex;
    justify-content: center;
    gap: 0.5rem;
    flex-wrap: wrap;
    padding: 0 1rem;
    margin-top: auto;
}

.tab-btn {
    background: none;
    border: none;
    color: rgba(249, 221, 211, 0.5);
    font-family: var(--font-label);
    font-size: 0.9rem;
    padding: 0.6rem 1.2rem;
    cursor: pointer;
    transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
    letter-spacing: 0.1em;
    border-radius: 20px;
}
.tab-btn:hover {
    color: var(--eat-secondary);
}
.tab-btn.active {
    color: var(--eat-primary);
    background-color: rgba(227, 199, 107, 0.1);
    box-shadow: inset 0 0 0 1px rgba(227, 199, 107, 0.3);
}

/* ── 搜尋 + 過濾列 ── */
.filter-bar {
    display: flex;
    flex-wrap: wrap;
    gap: 1rem;
    align-items: center;
    margin-bottom: 3rem;
}

.search-wrap {
    position: relative;
    flex: 1;
    min-width: 220px;
    max-width: 420px;
}
.search-icon {
    position: absolute;
    left: 0.75rem;
    top: 50%;
    transform: translateY(-50%);
    font-size: 0.85rem;
    opacity: 0.5;
}
.search-input {
    width: 100%;
    padding: 0.65rem 2.5rem 0.65rem 2.2rem;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(227, 199, 107, 0.15);
    border-radius: 30px;
    color: var(--eat-on-surface);
    font-family: var(--font-body);
    font-size: 0.9rem;
    outline: none;
    transition: border-color 0.3s;
}
.search-input:focus {
    border-color: rgba(227, 199, 107, 0.4);
}
.search-input::placeholder {
    color: rgba(249, 221, 211, 0.3);
}
.search-clear {
    position: absolute;
    right: 0.75rem;
    top: 50%;
    transform: translateY(-50%);
    background: none;
    border: none;
    color: rgba(249, 221, 211, 0.4);
    cursor: pointer;
    font-size: 0.75rem;
    padding: 0;
}

.filter-chips {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
}
.filter-chip {
    background: none;
    border: 1px solid rgba(227, 199, 107, 0.15);
    border-radius: 20px;
    color: rgba(249, 221, 211, 0.5);
    font-family: var(--font-label);
    font-size: 0.8rem;
    padding: 0.45rem 1rem;
    cursor: pointer;
    transition: all 0.3s;
    position: relative;
    overflow: hidden;
}
.ripple-wave {
    position: absolute;
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: rgba(227, 199, 107, 0.5);
    transform: translate(-50%, -50%) scale(0);
    animation: ripple-expand 0.55s ease-out forwards;
    pointer-events: none;
}
@keyframes ripple-expand {
    to {
        transform: translate(-50%, -50%) scale(20);
        opacity: 0;
    }
}
.filter-chip:hover {
    border-color: rgba(227, 199, 107, 0.35);
    color: rgba(249, 221, 211, 0.8);
}
.filter-chip.active {
    background: rgba(227, 199, 107, 0.12);
    border-color: rgba(227, 199, 107, 0.5);
    color: var(--eat-primary);
}

.sort-select {
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(227, 199, 107, 0.15);
    border-radius: 20px;
    color: var(--eat-on-surface);
    font-family: var(--font-label);
    font-size: 0.8rem;
    padding: 0.45rem 2rem 0.45rem 1rem;
    cursor: pointer;
    outline: none;
    transition: border-color 0.3s;
    appearance: none;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='10' height='6' viewBox='0 0 10 6'%3E%3Cpath d='M0 0l5 6 5-6z' fill='rgba(227,199,107,0.5)'/%3E%3C/svg%3E");
    background-repeat: no-repeat;
    background-position: right 0.75rem center;
}
.sort-select:hover {
    border-color: rgba(227, 199, 107, 0.35);
}
.sort-select:focus {
    border-color: rgba(227, 199, 107, 0.4);
}
.sort-select option {
    background: #1e100b;
    color: var(--eat-on-surface);
}
.filter-chip.active {
    background: rgba(227, 199, 107, 0.12);
    border-color: rgba(227, 199, 107, 0.5);
    color: var(--eat-primary);
}

/* ── 篩選統計列 ── */
.filter-stats {
    font-family: var(--font-label);
    font-size: 0.75rem;
    color: rgba(249, 221, 211, 0.4);
    letter-spacing: 0.15em;
    margin-bottom: 1.5rem;
    text-align: left;
}

/* ── 檢視模式切換按鈕 ── */
.view-toggle {
    display: flex;
    gap: 0.25rem;
}
.view-btn {
    background: none;
    border: 1px solid rgba(227, 199, 107, 0.15);
    border-radius: 8px;
    color: rgba(249, 221, 211, 0.5);
    font-size: 1rem;
    width: 2rem;
    height: 2rem;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.3s;
}
.view-btn:hover {
    border-color: rgba(227, 199, 107, 0.35);
    color: rgba(249, 221, 211, 0.8);
}
.view-btn.active {
    background: rgba(227, 199, 107, 0.12);
    border-color: rgba(227, 199, 107, 0.5);
    color: var(--eat-primary);
}

/* ── Grid ── */
.menu-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 2.5rem;
}

/* ── List ── */
.menu-list {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}
.menu-list .dish-card {
    flex-direction: row;
}
.menu-list .dish-img-wrap {
    width: 160px;
    min-width: 160px;
    height: 120px;
}
.menu-list .dish-info {
    padding: 1rem 1.25rem;
}
.menu-list .dish-desc {
    -webkit-line-clamp: unset;
    line-clamp: unset;
    display: block;
    overflow: visible;
}

/* ── Card 進場動畫 ── */
.card-enter-active {
    transition: opacity 0.35s ease;
}
.card-leave-active {
    transition: opacity 0.25s ease;
}
.card-enter-from,
.card-leave-to {
    opacity: 0;
}
.card-move {
    transition: transform 0.3s ease;
}

.dish-card {
    background-color: var(--eat-surface-high);
    border-radius: 16px;
    overflow: hidden;
    will-change: transform;
    transition:
        transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1),
        box-shadow 0.4s ease,
        border-color 0.3s;
    display: flex;
    flex-direction: column;
    position: relative;
    border: 1px solid rgba(227, 199, 107, 0.05);
    cursor: pointer;
}
.dish-card:hover {
    border-color: rgba(227, 199, 107, 0.2);
    transform: translateY(-10px);
    box-shadow:
        0 20px 48px rgba(0, 0, 0, 0.55),
        0 0 0 1px rgba(227, 199, 107, 0.18),
        0 0 24px rgba(227, 199, 107, 0.08);
}

/* ── Reflection layer ── */
.dish-card::before {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: inherit;
    background: radial-gradient(
        circle at var(--mx, 50%) var(--my, 50%),
        rgba(227, 199, 107, 0.1) 0%,
        rgba(227, 199, 107, 0.03) 40%,
        transparent 65%
    );
    opacity: 0;
    transition: opacity 0.3s ease;
    pointer-events: none;
    z-index: 4;
}
.dish-card:hover::before {
    opacity: 1;
}
.dish-card.is-soldout {
    opacity: 0.6;
    cursor: not-allowed;
}

/* ── 售完遮罩 ── */
.soldout-overlay {
    position: absolute;
    inset: 0;
    background: rgba(0, 0, 0, 0.55);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 2;
}
.soldout-text {
    color: white;
    font-size: 1.1rem;
    font-family: var(--font-label);
    letter-spacing: 0.2em;
}

/* ── Glare 反光塗層 ── */
.dish-card::after {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: inherit;
    /* 以 CSS 變數跟著游標移動，預設置中 */
    background: radial-gradient(
        circle at var(--glare-x, 50%) var(--glare-y, 50%),
        rgba(255, 255, 255, 0.12) 0%,
        rgba(255, 255, 255, 0.04) 40%,
        transparent 70%
    );
    opacity: 0;
    transition: opacity 0.3s ease;
    pointer-events: none;
    z-index: 3;
}
.dish-card:hover::after {
    opacity: 1;
}

/* ── 愛心收藏按鈕 ── */
.fav-btn {
    position: absolute;
    top: 0.75rem;
    right: 0.75rem;
    z-index: 3;
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

.dish-img-wrap {
    position: relative;
    height: 200px;
    overflow: hidden;
    background-color: #251813;
}
.dish-img-wrap img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: transform 1s ease;
}
.dish-card:hover .dish-img-wrap img {
    transform: scale(1.1);
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

.badge-group {
    position: absolute;
    top: 1rem;
    left: 1rem;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.4rem;
    z-index: 2;
}
.badge {
    padding: 0.2rem 0.6rem;
    border-radius: 4px;
    font-size: 0.65rem;
    font-family: var(--font-label);
    font-weight: 600;
    letter-spacing: 0.1em;
    backdrop-filter: blur(4px);
    display: inline-block;
    animation: badge-pop 0.5s cubic-bezier(0.34, 1.56, 0.64, 1) both;
    transition: transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.badge:hover {
    transform: scale(1.2) !important;
}
@keyframes badge-pop {
    from {
        opacity: 0;
        transform: scale(0.6);
    }
    to {
        opacity: 1;
        transform: scale(1);
    }
}
.badge-rec {
    background-color: rgba(227, 199, 107, 0.9);
    color: var(--eat-surface);
}
.badge-pop {
    background-color: rgba(217, 83, 79, 0.9);
    color: white;
}
.badge-veg {
    background-color: rgba(80, 160, 80, 0.85);
    color: white;
}
.badge-low-stock {
    background-color: rgba(200, 100, 0, 0.9);
    color: white;
}
.badge-lim {
    background-color: rgba(200, 40, 40, 0.92);
    color: white;
    white-space: nowrap;
}
.badge-soon {
    background-color: rgba(180, 120, 0, 0.9);
    color: white;
} /* 即將開始：琥珀橘 */
.badge-pending {
    background-color: rgba(60, 100, 180, 0.88);
    color: white;
} /* 尚未供應：藍灰 */
.badge-sold {
    background-color: rgba(80, 80, 80, 0.85);
    color: rgba(255, 255, 255, 0.55);
}

/* ── 限定供應倒數跳動特效 ── */
@keyframes pulse-urgency {
    0%,
    100% {
        transform: scale(1);
        box-shadow: 0 0 0 0 rgba(200, 40, 40, 0);
    }
    50% {
        transform: scale(1.08);
        box-shadow: 0 0 0 6px rgba(200, 40, 40, 0);
    }
    25% {
        box-shadow: 0 0 8px 3px rgba(200, 40, 40, 0.55);
    }
}

.fomo-timer {
    font-family: 'Courier New', Courier, monospace; /* 等寬字體：防止秒數跳動時文字位移 */
    letter-spacing: 0.05em;
    animation: pulse-urgency 2s ease-in-out infinite;
    /* 確保 transform-origin 在 badge 中心 */
    transform-origin: center center;
    display: inline-block;
}

.dish-info {
    padding: 1.5rem;
    flex-grow: 1;
    display: flex;
    flex-direction: column;
}
.dish-title-row {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 0.75rem;
}
.dish-name {
    font-family: var(--font-headline);
    color: var(--eat-primary);
    font-size: 1.25rem;
    margin: 0;
    font-style: italic;
    position: relative;
    display: inline-block;
}
.dish-name::after {
    content: '';
    position: absolute;
    left: 0;
    bottom: -2px;
    height: 1.5px;
    width: 0;
    background: var(--eat-primary);
    transition: width 0.35s cubic-bezier(0.4, 0, 0.2, 1);
}
.dish-card:hover .dish-name::after {
    width: 100%;
}
.dish-price-wrap {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 0.12rem;
    margin-left: 0.5rem;
    flex-shrink: 0;
}
.rating-tag {
    border: 1px solid rgba(227, 199, 107, 0.3);
    background: rgba(227, 199, 107, 0.07);
    color: var(--eat-primary);
    border-radius: 20px;
    padding: 0.15rem 0.55rem;
    font-family: var(--font-label);
    font-size: 0.72rem;
    letter-spacing: 0.03em;
}
.dish-price {
    font-family: var(--font-label);
    color: var(--eat-secondary);
    font-size: 0.9rem;
    font-weight: 500;
    margin-top: 0.25rem;
    white-space: nowrap;
    position: relative;
    transition: color 0.3s ease;
}
.dish-price::before {
    content: '';
    position: absolute;
    inset: -2px -6px;
    border-radius: 6px;
    background: rgba(227, 199, 107, 0.12);
    transform: scaleX(0);
    transform-origin: left;
    transition: transform 0.3s ease;
}
.dish-card:hover .dish-price {
    color: var(--eat-primary);
}
.dish-card:hover .dish-price::before {
    transform: scaleX(1);
}
.dish-desc {
    font-family: var(--font-body);
    font-size: 0.9rem;
    line-height: 1.7;
    color: rgba(249, 221, 211, 0.6);
    margin-bottom: 1.5rem;
    flex-grow: 1;
    font-style: italic;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}
.dish-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-top: 1px solid rgba(227, 199, 107, 0.1);
    padding-top: 1rem;
}
.dish-tags {
    display: flex;
    gap: 0.75rem;
}
.tag {
    font-size: 0.75rem;
}
.category-name {
    font-family: var(--font-label);
    font-size: 0.7rem;
    color: rgba(227, 199, 107, 0.4);
    text-transform: uppercase;
    letter-spacing: 0.1em;
}

/* ── Modal ── */
.modal-enter-active {
    transition: opacity 0.35s ease;
}
.modal-leave-active {
    transition: opacity 0.25s ease;
}
.modal-enter-from,
.modal-leave-to {
    opacity: 0;
}

.modal-overlay {
    position: fixed;
    inset: 0;
    z-index: 1000;
    background: rgba(0, 0, 0, 0.75);
    backdrop-filter: blur(16px);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1.5rem;
}

.modal-box {
    width: 100%;
    max-width: 520px;
    background: #1e100b;
    border: 1px solid rgba(227, 199, 107, 0.15);
    border-radius: 24px;
    overflow: hidden;
    animation: modalPop 0.45s cubic-bezier(0.34, 1.56, 0.64, 1);
}
@keyframes modalPop {
    from {
        opacity: 0;
        transform: scale(0.92) translateY(20px);
    }
    to {
        opacity: 1;
        transform: scale(1) translateY(0);
    }
}

.modal-img-wrap {
    position: relative;
    height: 240px;
    background: #251813;
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
    background: linear-gradient(135deg, #362620 0%, #1e100b 100%);
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
    height: 50%;
    background: linear-gradient(to top, #1e100b, transparent);
}
.modal-close {
    position: fixed;
    z-index: 1100;
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: rgba(0, 0, 0, 0.5);
    border: none;
    color: rgba(255, 255, 255, 0.8);
    font-size: 0.9rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: background 0.2s;
}
.modal-close:hover {
    background: rgba(0, 0, 0, 0.9);
}
.modal-badge-group {
    position: absolute;
    bottom: 1rem;
    left: 1.25rem;
    display: flex;
    gap: 0.4rem;
    z-index: 2;
}

.modal-body {
    padding: 1.75rem;
    max-height: 55vh;
    overflow-y: auto;
    scrollbar-width: thin;
    scrollbar-color: rgba(227, 199, 107, 0.2) transparent;
}
.modal-header-row {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 1rem;
    gap: 1rem;
}
.modal-title {
    font-family: var(--font-headline);
    color: var(--eat-primary);
    font-size: 1.8rem;
    font-style: italic;
    line-height: 1.2;
    margin: 0;
}
.modal-price {
    font-family: var(--font-label);
    color: var(--eat-secondary);
    font-size: 1.1rem;
    font-weight: 600;
    white-space: nowrap;
    padding-top: 0.35rem;
}
/* ── Modal 語意 Utility（對應 --eat-* 設計代號）── */
/* 色彩 Token */
.text-on-surface-variant {
    color: var(--eat-on-surface-variant);
}
.text-on-surface {
    color: var(--eat-on-surface);
}
.text-primary {
    color: var(--eat-primary);
}
/* 字體大小 */
.text-sm {
    font-size: 0.875rem;
    line-height: 1.5;
}
.text-xs {
    font-size: 0.75rem;
    line-height: 1.4;
}
.text-\[10px\] {
    font-size: 10px;
    line-height: 1.4;
}
/* 字體 Token */
.font-headline {
    font-family: var(--font-headline);
}
.font-body {
    font-family: var(--font-body);
}
/* 字重 / 樣式 */
.font-bold {
    font-weight: 700;
}
.italic {
    font-style: italic;
}
/* 間距 */
.tracking-widest {
    letter-spacing: 0.15em;
}
.mb-8 {
    margin-bottom: 2rem;
}
.mb-4 {
    margin-bottom: 1rem;
}
.mb-1 {
    margin-bottom: 0.25rem;
}
/* 顯示 */
.block {
    display: block;
}
/* Grid */
.grid {
    display: grid;
}
.grid-cols-2 {
    grid-template-columns: repeat(2, 1fr);
}
.gap-3 {
    gap: 0.75rem;
}
/* 背景 / 形狀 */
.bg-surface-container-low {
    background-color: var(--eat-surface-container);
}
.p-4 {
    padding: 1rem;
}
.rounded-xl {
    border-radius: 0.75rem;
}
.transition-all {
    transition:
        background-color 0.3s ease,
        transform 0.3s ease;
}
.bg-surface-container-low:hover {
    background-color: var(--eat-surface-high);
}

.modal-section {
    margin-bottom: 1.25rem;
}
.modal-section-label {
    font-family: var(--font-label);
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.15em;
    opacity: 0.4;
    margin-bottom: 0.65rem;
}
.attr-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
}
.attr-chip {
    font-size: 0.8rem;
    padding: 0.3rem 0.9rem;
    border-radius: 20px;
    border: 1px solid rgba(227, 199, 107, 0.2);
    color: rgba(249, 221, 211, 0.65);
    font-family: var(--font-label);
}
/* ── 評分星星 ── */
.star-row {
    display: flex;
    gap: 0.25rem;
    margin-bottom: 0.5rem;
}
.star-btn {
    font-size: 1.5rem;
    cursor: pointer;
    background: none;
    border: none;
    padding: 0;
    line-height: 1;
    color: var(--eat-primary);
    transition: transform 0.15s;
}
.star-btn:hover {
    transform: scale(1.2);
}

.rating-avg {
    font-family: var(--font-label);
    font-size: 0.78rem;
    color: var(--eat-primary);
    letter-spacing: 0.04em;
    margin: 0 0 0.6rem;
}
.rating-avg--empty {
    color: rgba(249, 221, 211, 0.35);
    font-style: italic;
}
.star-btn.is-rated {
    cursor: default;
    opacity: 0.75;
}
.star-btn:disabled {
    pointer-events: none;
}
.star-voted {
    font-family: var(--font-label);
    font-size: 0.7rem;
    color: rgba(249, 221, 211, 0.45);
    letter-spacing: 0.05em;
    margin: 0;
}

/* ── 評分總覽區塊 ── */
.rating-section-title {
    font-family: var(--font-label);
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.15em;
    color: rgba(249, 221, 211, 0.4);
    margin-bottom: 0.65rem;
    opacity: 1 !important;
}

/* 唯讀平均分星星列 */
.avg-star-row {
    display: flex;
    align-items: center;
    gap: 0.15rem;
    margin-bottom: 0.9rem;
}
.avg-star {
    position: relative;
    display: inline-block;
    font-size: 1.2rem;
    line-height: 1;
    width: 1.2rem;
    height: 1.2rem;
}
.avg-star-bg {
    color: rgba(227, 199, 107, 0.15);
}
.avg-star-fg {
    position: absolute;
    left: 0;
    top: 0;
    color: var(--eat-primary);
    overflow: hidden;
    white-space: nowrap;
    width: 0%;
}
.avg-star.full .avg-star-fg {
    width: 100%;
}
.avg-star.half .avg-star-fg {
    width: 50%;
}
.avg-star.empty .avg-star-fg {
    width: 0%;
}
.avg-star-label {
    font-family: var(--font-label);
    font-size: 0.75rem;
    color: var(--eat-primary);
    margin-left: 0.5rem;
    letter-spacing: 0.03em;
    opacity: 0.85;
}

/* 星數分佈條 */
.rating-bars {
    display: flex;
    flex-direction: column;
    gap: 0.28rem;
    margin-bottom: 0.5rem;
}
.rating-bar-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}
.bar-label {
    font-family: var(--font-label);
    font-size: 0.62rem;
    color: rgba(249, 221, 211, 0.4);
    width: 2rem;
    flex-shrink: 0;
    text-align: right;
}
.bar-track {
    flex: 1;
    height: 4px;
    background: rgba(227, 199, 107, 0.08);
    border-radius: 2px;
    overflow: hidden;
}
.bar-fill {
    height: 100%;
    background: linear-gradient(90deg, var(--eat-primary), rgba(227, 199, 107, 0.55));
    border-radius: 2px;
    transition: width 0.65s cubic-bezier(0.22, 1, 0.36, 1);
}
.rating-divider {
    height: 1px;
    background: rgba(227, 199, 107, 0.1);
    margin: 0.75rem 0 0;
}

.attr-chip.veg {
    border-color: rgba(80, 160, 80, 0.4);
    color: rgba(144, 238, 144, 0.8);
}
.attr-chip.spicy {
    border-color: rgba(217, 83, 79, 0.4);
    color: rgba(255, 140, 100, 0.9);
}
.attr-chip.rec {
    border-color: rgba(227, 199, 107, 0.4);
    color: var(--eat-primary);
}
.attr-chip.pop {
    border-color: rgba(217, 83, 79, 0.3);
    color: rgba(255, 140, 100, 0.8);
}

/* States */
.state-container {
    padding: 8rem 0;
    text-align: center;
}
.spinner {
    width: 48px;
    height: 48px;
    border: 2px solid rgba(227, 199, 107, 0.1);
    border-top-color: var(--eat-primary);
    border-radius: 50%;
    animation: spin 1s cubic-bezier(0.4, 0, 0.2, 1) infinite;
    margin: 0 auto 2rem;
}
.loading-text {
    font-family: var(--font-label);
    letter-spacing: 0.2em;
    color: var(--eat-secondary);
    font-size: 0.9rem;
}
.retry-btn {
    margin-top: 2rem;
    background: none;
    border: 1px solid var(--eat-primary);
    color: var(--eat-primary);
    padding: 0.6rem 2.5rem;
    font-family: var(--font-label);
    font-size: 0.8rem;
    cursor: pointer;
    transition: all 0.3s;
    text-transform: uppercase;
    letter-spacing: 0.2em;
}
.retry-btn:hover {
    background-color: var(--eat-primary);
    color: var(--eat-surface);
}
@keyframes spin {
    to {
        transform: rotate(360deg);
    }
}

/* Utils */
.container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 2rem;
}
.py-5 {
    padding-top: 2rem;
    padding-bottom: 6rem;
}

/* md:grid-cols-3 — 768px 以上升為三欄 */
@media (min-width: 768px) {
    .md\:grid-cols-3 {
        grid-template-columns: repeat(3, 1fr);
    }
}

/* ── Share button (同 SetMeal 樣式) ── */
.share-wrap {
    position: fixed;
    z-index: 1100;
}
.modal-share {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: rgba(0, 0, 0, 0.5);
    border: none;
    color: rgba(255, 255, 255, 0.8);
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition:
        background 0.2s,
        color 0.2s;
}
.modal-share:hover {
    background: rgba(0, 0, 0, 0.8);
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
    border-radius: 50%;
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

/* ── Ingredient AI panel ── */
.ingredient-hint {
    font-family: var(--font-body);
    font-size: 0.68rem;
    font-weight: 400;
    font-style: italic;
    opacity: 0.4;
    margin-left: 0.6rem;
    letter-spacing: 0.02em;
}
.ingredient-card {
    cursor: pointer;
    border: 1px solid transparent;
    transition:
        border-color 0.2s,
        background-color 0.2s,
        transform 0.2s;
}
.ingredient-card:hover {
    border-color: rgba(227, 199, 107, 0.2);
    transform: translateY(-2px);
}
.ingredient-card.is-active {
    border-color: rgba(227, 199, 107, 0.45);
    background-color: var(--eat-surface-high) !important;
}
.ingredient-info-panel {
    background: rgba(227, 199, 107, 0.04);
    border: 1px solid rgba(227, 199, 107, 0.15);
    border-radius: 12px;
    padding: 1rem 1.25rem;
}
.ingredient-info-title {
    font-family: var(--font-headline);
    font-style: italic;
    color: var(--eat-primary);
    font-size: 0.95rem;
    margin-bottom: 0.6rem;
}
.ingredient-loading {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    font-family: var(--font-label);
    font-size: 0.8rem;
    color: rgba(249, 221, 211, 0.5);
}
.ingredient-spinner {
    display: inline-block;
    width: 14px;
    height: 14px;
    border: 1.5px solid rgba(227, 199, 107, 0.2);
    border-top-color: var(--eat-primary);
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
    flex-shrink: 0;
}
.ingredient-info-text {
    font-family: var(--font-body);
    font-size: 0.82rem;
    line-height: 1.8;
    color: rgba(249, 221, 211, 0.75);
    white-space: pre-wrap;
    margin: 0;
}
.ingredient-panel-enter-active {
    transition:
        opacity 0.3s,
        transform 0.3s cubic-bezier(0.22, 1, 0.36, 1);
}
.ingredient-panel-leave-active {
    transition:
        opacity 0.2s,
        transform 0.2s;
}
.ingredient-panel-enter-from {
    opacity: 0;
    transform: translateY(-8px);
}
.ingredient-panel-leave-to {
    opacity: 0;
    transform: translateY(-4px);
}

/* ── Return to SetMeal button ── */
.return-setmeal-btn {
    position: fixed;
    bottom: 2rem;
    left: 2rem;
    z-index: 1100;
    background: rgba(30, 16, 10, 0.92);
    border: 1px solid rgba(227, 199, 107, 0.45);
    border-radius: 30px;
    color: var(--eat-primary);
    font-family: var(--font-label);
    font-size: 0.85rem;
    letter-spacing: 0.08em;
    padding: 0.55rem 1.4rem;
    cursor: pointer;
    backdrop-filter: blur(12px);
    transition:
        background 0.25s,
        border-color 0.25s,
        transform 0.2s;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.4);
}
.return-setmeal-btn:hover {
    background: rgba(227, 199, 107, 0.12);
    border-color: var(--eat-primary);
    transform: translateX(-2px);
}
.return-btn-enter-active {
    transition:
        opacity 0.3s,
        transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.return-btn-leave-active {
    transition:
        opacity 0.2s,
        transform 0.2s ease;
}
.return-btn-enter-from {
    opacity: 0;
    transform: translateX(-12px);
}
.return-btn-leave-to {
    opacity: 0;
    transform: translateX(-8px);
}

/* ── 留言區 ── */
.review-loading {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    font-family: var(--font-label);
    font-size: 0.8rem;
    color: rgba(249, 221, 211, 0.45);
    padding: 0.5rem 0;
}
.review-list {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
    margin-bottom: 0.25rem;
}
.review-item {
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(227, 199, 107, 0.1);
    border-radius: 10px;
    padding: 0.75rem 1rem;
}
.review-meta {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    margin-bottom: 0.35rem;
    gap: 0.5rem;
}
.review-nickname {
    font-family: var(--font-label);
    font-size: 0.78rem;
    color: var(--eat-primary);
    letter-spacing: 0.04em;
}
.review-time {
    font-family: var(--font-label);
    font-size: 0.65rem;
    color: rgba(249, 221, 211, 0.3);
    white-space: nowrap;
    flex-shrink: 0;
}
.review-content {
    font-family: var(--font-body);
    font-size: 0.83rem;
    line-height: 1.65;
    color: rgba(249, 221, 211, 0.72);
    margin: 0;
    word-break: break-word;
}
.review-more-btn {
    background: none;
    border: 1px solid rgba(227, 199, 107, 0.2);
    border-radius: 20px;
    color: rgba(227, 199, 107, 0.6);
    font-family: var(--font-label);
    font-size: 0.72rem;
    letter-spacing: 0.06em;
    padding: 0.35rem 1rem;
    cursor: pointer;
    transition:
        border-color 0.2s,
        color 0.2s;
    align-self: flex-start;
}
.review-more-btn:hover {
    border-color: rgba(227, 199, 107, 0.5);
    color: var(--eat-primary);
}
.review-empty {
    font-family: var(--font-body);
    font-size: 0.8rem;
    font-style: italic;
    color: rgba(249, 221, 211, 0.3);
    margin: 0.25rem 0;
}
.review-form {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}
.review-input,
.review-textarea {
    width: 100%;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid rgba(227, 199, 107, 0.15);
    border-radius: 10px;
    color: var(--eat-on-surface);
    font-family: var(--font-body);
    font-size: 0.85rem;
    padding: 0.6rem 0.9rem;
    outline: none;
    transition: border-color 0.25s;
    resize: none;
    box-sizing: border-box;
}
.review-input::placeholder,
.review-textarea::placeholder {
    color: rgba(249, 221, 211, 0.25);
}
.review-input:focus,
.review-textarea:focus {
    border-color: rgba(227, 199, 107, 0.4);
}
.review-form-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.75rem;
}
.review-char-count {
    font-family: var(--font-label);
    font-size: 0.65rem;
    color: rgba(249, 221, 211, 0.3);
    letter-spacing: 0.05em;
    flex-shrink: 0;
}
.review-submit-btn {
    background: rgba(227, 199, 107, 0.1);
    border: 1px solid rgba(227, 199, 107, 0.35);
    border-radius: 20px;
    color: var(--eat-primary);
    font-family: var(--font-label);
    font-size: 0.78rem;
    letter-spacing: 0.08em;
    padding: 0.45rem 1.25rem;
    cursor: pointer;
    transition:
        background 0.2s,
        border-color 0.2s;
    white-space: nowrap;
}
.review-submit-btn:hover:not(:disabled) {
    background: rgba(227, 199, 107, 0.2);
    border-color: var(--eat-primary);
}
.review-submit-btn:disabled {
    opacity: 0.35;
    cursor: not-allowed;
}

/* ── 登入提示 ──────────────────────────────────────── */
.login-hint {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.5rem 0;
}
.login-hint-text {
    font-family: var(--font-body);
    font-size: 0.8rem;
    font-style: italic;
    color: rgba(249, 221, 211, 0.35);
    margin: 0;
}
.login-hint-btn {
    background: none;
    border: 1px solid rgba(227, 199, 107, 0.28);
    border-radius: 20px;
    color: rgba(227, 199, 107, 0.7);
    font-family: var(--font-label);
    font-size: 0.72rem;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    padding: 0.3rem 0.9rem;
    cursor: pointer;
    transition:
        background 0.2s,
        border-color 0.2s,
        color 0.2s;
    white-space: nowrap;
    flex-shrink: 0;
}
.login-hint-btn:hover {
    background: rgba(227, 199, 107, 0.12);
    border-color: var(--eat-primary);
    color: var(--eat-primary);
}

@media (max-width: 768px) {
    .menu-header {
        padding-top: 6rem;
    }
    .eat-h1 {
        font-size: 2.2rem;
    }
    .menu-grid {
        grid-template-columns: 1fr;
    }
    .filter-bar {
        flex-direction: column;
        align-items: stretch;
    }
    .search-wrap {
        max-width: 100%;
    }
    .modal-title {
        font-size: 1.5rem;
    }
    .modal-body {
        max-height: 60vh;
    }
}
</style>
