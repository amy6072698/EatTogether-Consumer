<template>
    <!-- ══ 桌位驗證中 ══ -->
    <div v-if="tableStatus === 'checking'" class="gate-wrap">
        <div class="gate-spinner"></div>
        <p class="gate-hint font-label">驗證桌位中…</p>
    </div>

    <!-- ══ 禁止進入：無桌號 ══ -->
    <div v-else-if="tableStatus === 'no-table'" class="gate-wrap">
        <div class="gate-icon">🍽️</div>
        <h2 class="gate-title font-headline">歡迎光臨</h2>
        <p class="gate-body font-body">如需內用服務，請洽現場服務人員。</p>
    </div>

    <!-- ══ 禁止進入：空桌 / 找不到 ══ -->
    <div v-else-if="tableStatus === 'empty' || tableStatus === 'not-found'" class="gate-wrap">
        <div class="gate-icon">🪑</div>
        <h2 class="gate-title font-headline">此桌位目前為空桌</h2>
        <p class="gate-body font-body">如需服務請洽現場服務人員。</p>
        <p class="gate-sub font-label">桌號：{{ tableNameFromUrl }}</p>
    </div>

    <!-- ══ 禁止進入：保留中 ══ -->
    <div v-else-if="tableStatus === 'reserved'" class="gate-wrap">
        <div class="gate-icon">🔒</div>
        <h2 class="gate-title font-headline">此桌位已保留</h2>
        <p class="gate-body font-body">此桌位目前為訂位保留桌，如需服務請洽現場服務人員。</p>
        <p class="gate-sub font-label">桌號：{{ tableNameFromUrl }}</p>
    </div>

    <!-- ══ 正常點餐頁面 ══ -->
    <div v-else class="root-wrap">
        <!-- ══ 全寬頂部 Header ══ -->
        <header class="dine-header">
            <!-- 左：Logo + 今日菜單 -->
            <div class="dh-logo">
                <img src="@/assets/images/logo.svg" alt="義起吃" class="dh-logo-img" />
                <h1 class="dh-title font-headline">今日菜單</h1>
            </div>

            <!-- 中：彈性填充 -->
            <div class="dh-brand"></div>

            <!-- 右：桌號 ｜ 人數 ｜ 會員（同一行） -->
            <div class="dh-right">
                <span class="dh-label font-label">桌號</span>
                <span class="dh-table-name font-headline">{{
                    tables.find((t) => t.id === tableId)?.tableName || tableNameFromUrl || '—'
                }}</span>

                <div class="dh-sep"></div>

                <span :class="['dh-label', 'font-label', { 'pax-label-error': paxError }]"
                    >人數</span
                >
                <button
                    :class="['dh-pax-btn', 'font-label', { 'pax-ctrl-error': paxError }]"
                    :disabled="store.pax <= 1"
                    @click="store.pax = Math.max(1, store.pax - 1)"
                >
                    −
                </button>
                <input
                    type="number"
                    min="1"
                    max="99"
                    :class="[
                        'dh-pax-input',
                        'font-label',
                        { 'pax-empty': store.pax === 0, 'pax-input-error': paxError },
                    ]"
                    :value="store.pax || ''"
                    placeholder="—"
                    @change="
                        store.pax = Math.max(0, Math.min(99, parseInt($event.target.value) || 0))
                    "
                />
                <button
                    :class="['dh-pax-btn', 'font-label', { 'pax-ctrl-error': paxError }]"
                    @click="store.pax = (store.pax || 0) + 1"
                >
                    +
                </button>

                <div class="dh-sep"></div>

                <!-- 會員 -->
                <div
                    class="dh-member dh-member-btn"
                    @click.stop="
                        isLoggedIn ? (memberDropdownOpen = !memberDropdownOpen) : openAuthModal()
                    "
                >
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="13"
                        height="13"
                        fill="currentColor"
                        style="color: rgba(208, 197, 181, 0.45); flex-shrink: 0"
                        viewBox="0 0 16 16"
                    >
                        <path
                            d="M8 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6zm2-3a2 2 0 1 1-4 0 2 2 0 0 1 4 0zm4 8c0 1-1 1-1 1H3s-1 0-1-1 1-4 6-4 6 3 6 4zm-1-.004c-.001-.246-.154-.986-.832-1.664C11.516 10.68 10.029 10 8 10c-2.03 0-3.516.68-4.168 1.332-.678.678-.83 1.418-.832 1.664h10z"
                        />
                    </svg>
                    <span class="dh-member-label font-label">{{ memberName }}</span>
                    <svg
                        class="dh-chevron"
                        :class="{ open: memberDropdownOpen }"
                        xmlns="http://www.w3.org/2000/svg"
                        width="9"
                        height="9"
                        fill="currentColor"
                        viewBox="0 0 16 16"
                    >
                        <path
                            d="M7.247 11.14L2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"
                        />
                    </svg>
                    <!-- 下拉選單 -->
                    <div
                        v-if="memberDropdownOpen && isLoggedIn"
                        class="member-dropdown"
                        @click.stop=""
                    >
                        <button class="member-dropdown-item font-label" @click="openAuthModal">
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                width="13"
                                height="13"
                                fill="currentColor"
                                viewBox="0 0 16 16"
                            >
                                <path
                                    fill-rule="evenodd"
                                    d="M1 8a7 7 0 1 0 14 0A7 7 0 0 0 1 8zm15 0A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-4.5-.5a.5.5 0 0 1 0 1H5.707l2.147 2.146a.5.5 0 0 1-.708.708l-3-3a.5.5 0 0 1 0-.708l3-3a.5.5 0 1 1 .708.708L5.707 7.5H11.5z"
                                />
                            </svg>
                            切換帳號
                        </button>
                    </div>
                </div>
            </div>

            <!-- 點擊背景關閉下拉 -->
            <div
                v-if="memberDropdownOpen"
                class="member-backdrop"
                @click="memberDropdownOpen = false"
            ></div>
        </header>

        <!-- ══ ----手機板---- 專用 Header（Grid：Logo 跨兩行） ══ -->
        <header class="dine-header-mobile">
            <!-- Grid 結構：
                 [logo][  row1: 今日菜單  |  會員  ]
                 [logo][  row2: 桌號 xx   |  人數  ]
            -->

            <!-- 左：Logo（grid-row: span 2） -->
            <img src="@/assets/images/logo.svg" alt="義起吃" class="dhm-logo-img" />

            <!-- 右上：今日菜單+ 會員（靠右） -->
            <div class="dhm-row1">
                <h1 class="dhm-title font-headline">今日菜單</h1>
                <div
                    class="dh-member dh-member-btn"
                    @click.stop="
                        isLoggedIn ? (memberDropdownOpen = !memberDropdownOpen) : openAuthModal()
                    "
                >
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="13"
                        height="13"
                        fill="currentColor"
                        style="color: rgba(208, 197, 181, 0.45); flex-shrink: 0"
                        viewBox="0 0 16 16"
                    >
                        <path
                            d="M8 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6zm2-3a2 2 0 1 1-4 0 2 2 0 0 1 4 0zm4 8c0 1-1 1-1 1H3s-1 0-1-1 1-4 6-4 6 3 6 4zm-1-.004c-.001-.246-.154-.986-.832-1.664C11.516 10.68 10.029 10 8 10c-2.03 0-3.516.68-4.168 1.332-.678.678-.83 1.418-.832 1.664h10z"
                        />
                    </svg>
                    <span class="dh-member-label font-label">{{ memberName }}</span>
                    <svg
                        class="dh-chevron"
                        :class="{ open: memberDropdownOpen }"
                        xmlns="http://www.w3.org/2000/svg"
                        width="9"
                        height="9"
                        fill="currentColor"
                        viewBox="0 0 16 16"
                    >
                        <path
                            d="M7.247 11.14L2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"
                        />
                    </svg>
                    <div
                        v-if="memberDropdownOpen && isLoggedIn"
                        class="member-dropdown"
                        @click.stop=""
                    >
                        <button class="member-dropdown-item font-label" @click="openAuthModal">
                            <svg
                                xmlns="http://www.w3.org/2000/svg"
                                width="13"
                                height="13"
                                fill="currentColor"
                                viewBox="0 0 16 16"
                            >
                                <path
                                    fill-rule="evenodd"
                                    d="M1 8a7 7 0 1 0 14 0A7 7 0 0 0 1 8zm15 0A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-4.5-.5a.5.5 0 0 1 0 1H5.707l2.147 2.146a.5.5 0 0 1-.708.708l-3-3a.5.5 0 0 1 0-.708l3-3a.5.5 0 1 1 .708.708L5.707 7.5H11.5z"
                                />
                            </svg>
                            切換帳號
                        </button>
                    </div>
                </div>
            </div>

            <!-- 右下：桌號 + 用餐人數 -->
            <div class="dhm-row2">
                <span class="dhm-label font-label">桌號</span>
                <span class="dhm-table-name font-headline">{{
                    tables.find((t) => t.id === tableId)?.tableName || tableNameFromUrl || '—'
                }}</span>
                <div class="dh-sep"></div>
                <span :class="['dhm-label', 'font-label', { 'pax-label-error': paxError }]"
                    >用餐人數</span
                >
                <button
                    :class="['dh-pax-btn', 'font-label', { 'pax-ctrl-error': paxError }]"
                    :disabled="store.pax <= 1"
                    @click="store.pax = Math.max(1, store.pax - 1)"
                >
                    −
                </button>
                <input
                    type="number"
                    min="1"
                    max="99"
                    :class="[
                        'dh-pax-input',
                        'font-label',
                        { 'pax-empty': store.pax === 0, 'pax-input-error': paxError },
                    ]"
                    :value="store.pax || ''"
                    placeholder="—"
                    @change="
                        store.pax = Math.max(0, Math.min(99, parseInt($event.target.value) || 0))
                    "
                />
                <button
                    :class="['dh-pax-btn', 'font-label', { 'pax-ctrl-error': paxError }]"
                    @click="store.pax = (store.pax || 0) + 1"
                >
                    +
                </button>
            </div>

            <!-- 點擊背景關閉下拉 -->
            <div
                v-if="memberDropdownOpen"
                class="member-backdrop"
                @click="memberDropdownOpen = false"
            ></div>
        </header>

        <!-- ══ 手機分類捲動列 ══ -->
        <header class="mobile-header">
            <nav class="mobile-cat-tabs">
                <button
                    v-for="cat in sidebarCategories"
                    :key="cat.key"
                    @click="scrollToSection(cat.key)"
                    :class="['mobile-cat-btn', { active: activeSidebarCat === cat.key }]"
                >
                    {{
                        cat.key === '今日推薦'
                            ? '今日推薦'
                            : cat.key === '主廚特選'
                              ? '主廚特選'
                              : cat.label
                    }}<span class="cat-badge">{{ cat.count }}</span>
                </button>
            </nav>
        </header>

        <div class="page-layout">
            <!-- ── LEFT Sidebar（桌機） ── -->
            <aside class="sidebar-left flex flex-col">
                <div class="px-4 py-4" style="border-bottom: 1px solid rgba(77, 70, 58, 0.2)">
                    <div class="relative w-full gap-1" style="display: flex; align-items: center">
                        <svg
                            class="absolute left-4 top-1/2 -translate-y-1/2"
                            style="color: rgba(208, 197, 181, 0.4)"
                            xmlns="http://www.w3.org/2000/svg"
                            width="13"
                            height="13"
                            fill="currentColor"
                            viewBox="0 0 16 16"
                        >
                            <path
                                d="M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001c.03.04.062.078.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1.007 1.007 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0z"
                            />
                        </svg>
                        <input
                            v-model="searchQuery"
                            type="text"
                            class="input-line w-full"
                            style="padding-left: 0.8rem; font-size: 0.85rem"
                            placeholder="搜尋料理名稱…"
                        />
                    </div>
                </div>
                <nav class="flex-1 py-3 overflow-y-auto">
                    <template v-for="(cat, idx) in sidebarCategories" :key="cat.key">
                        <!-- 特殊分類(今日推薦/主廚特選)與一般分類(全部/套餐…)之間的分隔線 -->
                        <div
                            v-if="
                                cat.isDividerBefore ||
                                (idx > 0 &&
                                    !['今日推薦', '主廚特選', '我的收藏', '歷史訂單'].includes(
                                        cat.key
                                    ) &&
                                    ['今日推薦', '主廚特選', '我的收藏', '歷史訂單'].includes(
                                        sidebarCategories[idx - 1].key
                                    ))
                            "
                            class="cat-divider"
                        ></div>
                        <button
                            @click="scrollToSection(cat.key)"
                            :class="[
                                'cat-link w-full text-left',
                                { active: activeSidebarCat === cat.key },
                                {
                                    'cat-special': [
                                        '今日推薦',
                                        '主廚特選',
                                        '我的收藏',
                                        '歷史訂單',
                                    ].includes(cat.key),
                                },
                            ]"
                        >
                            <span>{{
                                cat.key === '今日推薦'
                                    ? '今日推薦'
                                    : cat.key === '主廚特選'
                                      ? '主廚特選'
                                      : cat.key === '我的收藏'
                                        ? '我的收藏'
                                        : cat.key === '歷史訂單'
                                          ? '歷史訂單'
                                          : cat.label
                            }}</span>
                            <span class="cat-count">{{ cat.count }}</span>
                        </button>
                    </template>
                </nav>
            </aside>

            <!-- ── CENTRE: Menu ── -->
            <main class="menu-main min-w-0" id="menuMain">
                <!-- Toolbar -->
                <div class="toolbar">
                    <div class="mobile-search-wrap">
                        <svg
                            style="color: rgba(208, 197, 181, 0.4); flex-shrink: 0"
                            xmlns="http://www.w3.org/2000/svg"
                            width="13"
                            height="13"
                            fill="currentColor"
                            viewBox="0 0 16 16"
                        >
                            <path
                                d="M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001c.03.04.062.078.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1.007 1.007 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0z"
                            />
                        </svg>
                        <input
                            v-model="searchQuery"
                            type="text"
                            class="input-line"
                            style="font-size: 0.85rem"
                            placeholder="搜尋料理名稱…"
                        />
                    </div>
                    <div class="toolbar-row">
                        <div class="chips-wrap">
                            <span class="chip-label">篩選條件：</span>
                            <button
                                @click="toggleChip('veg')"
                                :class="['chip-btn', { active: activeChip === 'veg' }]"
                            >
                                素食
                            </button>
                            <button
                                @click="toggleChip('spicy')"
                                :class="['chip-btn', { active: activeChip === 'spicy' }]"
                            >
                                辣味
                            </button>
                        </div>
                        <div class="view-toggle">
                            <button
                                @click="curView = 'list'"
                                :class="['view-btn', { active: curView === 'list' }]"
                            >
                                <svg
                                    xmlns="http://www.w3.org/2000/svg"
                                    width="13"
                                    height="13"
                                    fill="currentColor"
                                    viewBox="0 0 16 16"
                                >
                                    <path
                                        fill-rule="evenodd"
                                        d="M2.5 12a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5z"
                                    />
                                </svg>
                            </button>
                            <button
                                @click="curView = 'grid'"
                                :class="['view-btn', { active: curView === 'grid' }]"
                            >
                                <svg
                                    xmlns="http://www.w3.org/2000/svg"
                                    width="13"
                                    height="13"
                                    fill="currentColor"
                                    viewBox="0 0 16 16"
                                >
                                    <path
                                        d="M1 2.5A1.5 1.5 0 0 1 2.5 1h3A1.5 1.5 0 0 1 7 2.5v3A1.5 1.5 0 0 1 5.5 7h-3A1.5 1.5 0 0 1 1 5.5v-3zm8 0A1.5 1.5 0 0 1 10.5 1h3A1.5 1.5 0 0 1 15 2.5v3A1.5 1.5 0 0 1 13.5 7h-3A1.5 1.5 0 0 1 9 5.5v-3zm-8 8A1.5 1.5 0 0 1 2.5 9h3A1.5 1.5 0 0 1 7 10.5v3A1.5 1.5 0 0 1 5.5 15h-3A1.5 1.5 0 0 1 1 13.5v-3zm8 0A1.5 1.5 0 0 1 10.5 9h3A1.5 1.5 0 0 1 15 10.5v3A1.5 1.5 0 0 1 13.5 15h-3A1.5 1.5 0 0 1 9 13.5v-3z"
                                    />
                                </svg>
                            </button>
                        </div>
                    </div>
                </div>

                <div v-if="loading" class="status-msg">菜單載入中…</div>
                <div v-else-if="loadError" class="status-msg" style="color: #ffb4ab">
                    {{ loadError }}
                </div>

                <div v-else class="menu-sections">
                    <template v-for="section in displaySections" :key="section.key">
                        <div v-show="section.dishes.length > 0" :id="'sec-' + section.key">
                            <h2 class="section-title">{{ section.label }}</h2>
                            <div class="dishes-wrap" :class="{ 'grid-view': curView === 'grid' }">
                                <div
                                    v-for="dish in section.dishes"
                                    :key="dish.productId"
                                    class="dish-row mb-3"
                                    :class="{ 'dish-row-grid': curView === 'grid' }"
                                    @click="openDetail(dish)"
                                >
                                    <img
                                        v-if="dish.imageUrl && !imgErrors.has(dish.productId)"
                                        class="dish-img"
                                        :src="
                                            imgFallback.get(dish.productId) ||
                                            resolveImage(dish.imageUrl)
                                        "
                                        :alt="dish.productName"
                                        @error="handleImgError(dish)"
                                    />
                                    <div v-else class="dish-img dish-img-placeholder">🍽️</div>
                                    <div
                                        class="dish-content min-w-0"
                                        :class="{ 'dish-content-grid': curView === 'grid' }"
                                    >
                                        <!-- 餐點名稱 -->
                                        <h3
                                            class="font-headline dish-name italic"
                                            style="color: #e3c76b"
                                        >
                                            {{ dish.productName }}
                                        </h3>
                                        <!-- 描述 -->
                                        <p
                                            class="font-body text-xs italic leading-relaxed"
                                            style="color: rgba(249, 221, 211, 0.5); margin: 0"
                                        >
                                            {{ dish.description }}
                                        </p>
                                        <!-- 列表視圖：badge + 價格 & 按鈕並排 -->
                                        <template v-if="curView !== 'grid'">
                                            <div class="dish-badges">
                                                <span
                                                    v-if="dish.isRecommended"
                                                    class="badge badge-new"
                                                    >推薦</span
                                                >
                                                <span v-if="dish.isPopular" class="badge badge-chef"
                                                    >主廚特選</span
                                                >
                                                <span
                                                    v-if="dish.isVegetarian"
                                                    class="badge badge-veg"
                                                    >素</span
                                                >
                                                <span
                                                    v-if="dish.spicyLevel > 0"
                                                    class="badge badge-spicy"
                                                    >辣</span
                                                >
                                            </div>
                                            <!-- 價格 ＋ 數量按鈕同一列 -->
                                            <div class="list-footer" @click.stop="">
                                                <p
                                                    class="font-label text-xs tracking-wider"
                                                    style="color: #d5b478"
                                                >
                                                    NT$ {{ dish.unitPrice?.toLocaleString() }}
                                                </p>
                                                <div class="qty-col">
                                                    <button
                                                        class="qty-btn"
                                                        @click.stop="handleRemove(dish)"
                                                    >
                                                        −
                                                    </button>
                                                    <span
                                                        class="font-label text-xs qty-num"
                                                        :class="{
                                                            active:
                                                                (store.cart[dish.productId] || 0) >
                                                                0,
                                                        }"
                                                        style="font-size: 20px"
                                                        >{{ store.cart[dish.productId] || 0 }}</span
                                                    >
                                                    <button
                                                        class="qty-btn"
                                                        @click.stop="openDetail(dish)"
                                                    >
                                                        +
                                                    </button>
                                                </div>
                                            </div>
                                        </template>
                                    </div>
                                    <!-- 網格視圖：badge → 價格 → 按鈕，全部固定在底部置中 -->
                                    <div
                                        v-if="curView === 'grid'"
                                        class="grid-footer"
                                        @click.stop=""
                                    >
                                        <div class="dish-badges" style="justify-content: center">
                                            <span v-if="dish.isRecommended" class="badge badge-new"
                                                >推薦</span
                                            >
                                            <span v-if="dish.isPopular" class="badge badge-chef"
                                                >主廚特選</span
                                            >
                                            <span v-if="dish.isVegetarian" class="badge badge-veg"
                                                >素</span
                                            >
                                            <span
                                                v-if="dish.spicyLevel > 0"
                                                class="badge badge-spicy"
                                                >辣</span
                                            >
                                        </div>
                                        <p class="font-label grid-price mb-2">
                                            NT$ {{ dish.unitPrice?.toLocaleString() }}
                                        </p>
                                        <div class="qty-row">
                                            <button
                                                class="qty-btn"
                                                @click.stop="handleRemove(dish)"
                                            >
                                                −
                                            </button>
                                            <span
                                                class="font-label text-xs qty-num"
                                                :class="{
                                                    active: (store.cart[dish.productId] || 0) > 0,
                                                }"
                                                style="font-size: 20px"
                                                >{{ store.cart[dish.productId] || 0 }}</span
                                            >
                                            <button class="qty-btn" @click.stop="openDetail(dish)">
                                                +
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="feather-divider"></div>
                        </div>
                    </template>

                    <!-- 歷史訂單 -->
                    <template v-if="activeSidebarCat === '歷史訂單'">
                        <h2 class="section-title">歷史訂單</h2>
                        <div
                            v-for="order in orderHistory"
                            :key="order.orderNumber"
                            class="history-card"
                        >
                            <div class="history-meta">
                                <span
                                    class="font-label"
                                    style="color: rgba(208, 197, 181, 0.5); font-size: 0.7rem"
                                    >訂單編號：{{ order.orderNumber }}</span
                                >
                                <span
                                    class="font-label"
                                    style="color: rgba(208, 197, 181, 0.5); font-size: 0.7rem"
                                >
                                    下單時間：{{
                                        new Date(order.orderAt).toLocaleString('zh-TW', {
                                            year: 'numeric',
                                            month: '2-digit',
                                            day: '2-digit',
                                            hour: '2-digit',
                                            minute: '2-digit',
                                        })
                                    }}
                                </span>
                            </div>
                            <div class="history-items">
                                <div
                                    v-for="item in order.items?.filter((i) => i)"
                                    :key="item.productName"
                                    class="history-item-wrap"
                                >
                                    <div
                                        class="font-body"
                                        style="color: #f9ddd3; font-size: 0.9rem"
                                    >
                                        <span
                                            v-if="item.isSetMeal"
                                            class="history-setmeal-badge font-label"
                                            >套餐</span
                                        >
                                        {{ item.qty }} x {{ item.productName }}
                                        <span
                                            v-if="item.note"
                                            style="
                                                color: rgba(208, 197, 181, 0.5);
                                                font-size: 0.8rem;
                                            "
                                            >（{{ item.note }}）</span
                                        >
                                    </div>
                                    <!-- 套餐子項目 -->
                                    <div
                                        v-if="item.isSetMeal && item.subItems?.length"
                                        class="history-subitems"
                                    >
                                        <span
                                            v-for="sub in item.subItems"
                                            :key="sub.productName"
                                            class="font-body history-subitem"
                                            >{{ sub.qty }} x {{ sub.productName }}</span
                                        >
                                    </div>
                                </div>
                                <!-- 整單備註 -->
                                <div
                                    v-if="order.orderNote"
                                    class="font-body"
                                    style="
                                        color: rgba(208, 197, 181, 0.5);
                                        font-size: 0.8rem;
                                        margin-top: 0.25rem;
                                    "
                                >
                                    備註：{{ order.orderNote }}
                                </div>
                            </div>
                            <button class="history-reorder-btn font-label" @click="reorder(order)">
                                再點一次
                            </button>
                        </div>
                    </template>
                </div>
            </main>

            <!-- ── RIGHT: Order Panel（Teleport 到 body，避免 root-wrap overflow:hidden 影響 z-index） -->
            <Teleport to="body">
                <aside class="order-panel" :class="{ 'panel-open': orderPanelOpen }">
                    <!-- 標題列 -->
                    <div class="panel-header candle-glow">
                        <span class="font-headline" style="color: #e3c76b; font-size: 1.1rem"
                            >我的訂單</span
                        >
                        <div style="display: flex; align-items: center; gap: 0.75rem">
                            <span
                                class="font-label"
                                style="
                                    color: rgba(208, 197, 181, 0.5);
                                    font-size: 0.75rem;
                                    letter-spacing: 0.12em;
                                "
                                >共 {{ store.totalItems }} 項</span
                            >
                            <button @click="orderPanelOpen = false" class="panel-close-btn">
                                ✕
                            </button>
                        </div>
                    </div>

                    <!-- 餐點清單（可捲動） -->
                    <div class="panel-items">
                        <div
                            v-if="store.totalItems === 0"
                            style="text-align: center; padding: 3rem 1rem"
                        >
                            <p style="font-size: 2.5rem; opacity: 0.15; margin-bottom: 0.75rem">
                                🍽️
                            </p>
                            <p class="font-body" style="color: rgba(249, 221, 211, 0.4)">
                                尚未點選任何餐點
                            </p>
                            <p
                                class="font-label"
                                style="
                                    color: rgba(208, 197, 181, 0.35);
                                    font-size: 0.7rem;
                                    letter-spacing: 0.18em;
                                    text-transform: uppercase;
                                    margin-top: 0.5rem;
                                "
                            >
                                點擊料理即可加入
                            </p>
                        </div>
                        <div
                            v-for="item in cartItemsWithDetails"
                            :key="item.lineId"
                            :class="['order-item', { 'setmeal-order-item': item.isSetMeal }]"
                        >
                            <!-- 套餐 badge 獨占一行 -->
                            <div v-if="item.isSetMeal" class="setmeal-badge font-label">
                                🍱 套餐
                            </div>

                            <div class="order-item-top">
                                <!-- 左側名稱區：點擊開啟編輯 Modal -->
                                <div
                                    class="order-item-name-wrap"
                                    @click="openCartItemEdit(item)"
                                    title="點擊編輯"
                                >
                                    <p class="font-headline order-item-name" style="color: #e3c76b">
                                        {{ item.productName }}
                                    </p>
                                    <p v-if="item.note" class="font-body order-item-note">
                                        {{ item.note }}
                                    </p>
                                </div>
                                <div class="order-item-right">
                                    <span class="font-label order-item-price"
                                        >NT$
                                        {{ (item.unitPrice * item.qty).toLocaleString() }}</span
                                    >
                                    <button
                                        class="qty-btn-order"
                                        @click.stop="store.removeLineItem(item.lineId)"
                                    >
                                        −
                                    </button>
                                    <span
                                        class="font-label order-item-qty"
                                        style="color: #f9ddd3"
                                        >{{ item.qty }}</span
                                    >
                                    <button
                                        v-if="!item.isSetMeal"
                                        class="qty-btn-order"
                                        @click.stop="store.addItem(item.productId, item.note)"
                                    >
                                        +
                                    </button>
                                    <button
                                        v-else
                                        class="qty-btn-order"
                                        @click.stop="store.addLineItem(item.lineId)"
                                    >
                                        +
                                    </button>
                                </div>
                            </div>

                            <!-- 套餐子項目：每項獨立一行，名稱靠左 × qty 靠右 -->
                            <div v-if="item.isSetMeal" class="setmeal-subitems">
                                <div
                                    v-for="f in item.setMealData?.fixedItems"
                                    :key="'f-' + f.dishId"
                                    class="setmeal-subitem-row"
                                >
                                    <span class="font-label setmeal-subitem-name">{{
                                        f.dishName
                                    }}</span>
                                    <span class="font-label setmeal-subitem-qty"
                                        >× {{ f.quantity }}</span
                                    >
                                </div>
                                <div
                                    v-for="s in item.setMealData?.selectedOptions"
                                    :key="'s-' + s.dishId"
                                    class="setmeal-subitem-row"
                                >
                                    <span class="font-label setmeal-subitem-name">{{
                                        s.dishName
                                    }}</span>
                                    <span class="font-label setmeal-subitem-qty"
                                        >× {{ s.qty }}</span
                                    >
                                </div>
                            </div>
                        </div>

                        <!-- 贈品列（Gift 型活動自動帶入，唯讀） -->
                        <div v-if="isLoggedIn && giftCartItem" class="order-item gift-order-item">
                            <div class="order-item-top">
                                <div style="display: flex; flex-direction: column; gap: 0.1rem">
                                    <p class="font-headline order-item-name" style="color: #e3c76b">
                                        {{ giftCartItem.productName }}
                                    </p>
                                </div>
                                <div class="order-item-right">
                                    <span class="gift-order-badge font-label">🎁 贈品</span>
                                    <span
                                        class="font-label order-item-qty"
                                        style="
                                            color: rgba(208, 197, 181, 0.55);
                                            font-size: 0.75rem;
                                            min-width: 1.2rem;
                                            text-align: center;
                                        "
                                        >× 1</span
                                    >
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- 固定底部：備註 + 合計 + 按鈕 -->
                    <div class="panel-footer">
                        <div
                            style="
                                padding: 0.75rem 1rem;
                                border-bottom: 1px solid rgba(77, 70, 58, 0.2);
                            "
                        >
                            <textarea
                                v-model="store.specialRequest"
                                class="note-textarea font-body resize-none"
                                rows="2"
                                placeholder="備註：過敏食材、特殊需求…"
                            ></textarea>

                            <!-- 通知型活動（IsAutoDiscount=0）→ 改用 Toast 通知，見下方 Teleport -->

                            <!-- 優惠券：已登入顯示下拉，未登入顯示文字輸入 -->
                            <div style="margin-top: 0.5rem">
                                <!-- 已登入：下拉選已領優惠券 -->
                                <template v-if="isLoggedIn">
                                    <select
                                        v-model="selectedCouponCode"
                                        @change="onCouponSelect"
                                        class="coupon-select font-body"
                                    >
                                        <option value="">— 選擇優惠券 —</option>
                                        <option
                                            v-for="c in availableCoupons"
                                            :key="c.id"
                                            :value="c.code"
                                        >
                                            {{ c.couponName }}｜{{ c.discountDescription }}
                                            <template v-if="c.endDate">
                                                （{{
                                                    new Date(c.endDate).toLocaleDateString('zh-TW')
                                                }}
                                                到期）
                                            </template>
                                        </option>
                                        <!-- 最後一項：導到優惠券專區 -->
                                        <option value="__coupons__" class="coupon-goto-option">
                                            ✦ 查看更多優惠券 →
                                        </option>
                                    </select>
                                    <p
                                        v-if="couponMsg"
                                        class="font-label mb-0"
                                        style="font-size: 0.7rem; margin-top: 0.25rem"
                                        :style="{ color: couponOk ? '#a3d977' : '#ffb4ab' }"
                                    >
                                        {{ couponMsg }}
                                    </p>
                                </template>

                                <!-- 未登入：提示登入才能使用優惠券 -->
                                <template v-else>
                                    <button
                                        @click="openAuthModal"
                                        class="font-label coupon-login-btn"
                                    >
                                        立即登入會員享優惠
                                    </button>
                                </template>
                            </div>
                        </div>
                        <div
                            style="
                                padding: 0.75rem 1rem;
                                display: flex;
                                flex-direction: column;
                                gap: 0.5rem;
                            "
                        >
                            <div style="display: flex; flex-direction: column; gap: 0.3rem">
                                <!-- 已套用活動標籤（已登入會員） -->
                                <div v-if="isLoggedIn && bestAutoEvent" class="applied-event-tag">
                                    <span class="applied-event-icon">🎉</span>
                                    <span class="font-label applied-event-text">
                                        已參加「{{ bestAutoEvent.title }}」活動 （滿 NT${{
                                            bestAutoEvent.minSpend
                                        }}
                                        可享 {{ bestAutoEvent.discountDescription }}）
                                    </span>
                                </div>
                                <!-- 活動折扣列（FixedAmount / Percent 才顯示，Gift 不扣金額） -->
                                <div
                                    v-if="isLoggedIn && autoEventDiscount > 0"
                                    style="
                                        display: flex;
                                        justify-content: space-between;
                                        align-items: center;
                                    "
                                >
                                    <span
                                        class="font-label"
                                        style="
                                            font-size: 0.8rem;
                                            color: rgba(163, 217, 119, 0.8);
                                            letter-spacing: 0.06em;
                                        "
                                        >活動折扣</span
                                    >
                                    <span
                                        class="font-label"
                                        style="
                                            font-size: 0.8rem;
                                            color: #a3d977;
                                            letter-spacing: 0.06em;
                                        "
                                        >－ NT$ {{ autoEventDiscount.toLocaleString() }}</span
                                    >
                                </div>
                                <!-- 優惠券折扣列 -->
                                <div
                                    v-if="couponOk && couponDiscount > 0"
                                    style="
                                        display: flex;
                                        justify-content: space-between;
                                        align-items: center;
                                    "
                                >
                                    <span
                                        class="font-label"
                                        style="
                                            font-size: 0.8rem;
                                            color: rgba(163, 217, 119, 0.8);
                                            letter-spacing: 0.06em;
                                        "
                                        >優惠券折扣</span
                                    >
                                    <span
                                        class="font-label"
                                        style="
                                            font-size: 0.8rem;
                                            color: #a3d977;
                                            letter-spacing: 0.06em;
                                        "
                                        >－ NT$ {{ couponDiscount.toLocaleString() }}</span
                                    >
                                </div>
                                <!-- 合計列 -->
                                <div
                                    style="
                                        display: flex;
                                        justify-content: space-between;
                                        align-items: center;
                                    "
                                >
                                    <span
                                        class="font-headline"
                                        style="color: #e3c76b; font-size: 1.1rem"
                                        >合計</span
                                    >
                                    <span
                                        class="font-label"
                                        style="
                                            color: #e3c76b;
                                            font-size: 1.1rem;
                                            letter-spacing: 0.08em;
                                        "
                                        >NT$ {{ finalTotal.toLocaleString() }}</span
                                    >
                                </div>
                            </div>
                            <button
                                @click="submitOrder"
                                :disabled="store.totalItems === 0 || submitting"
                                class="submit-btn font-label"
                                style="
                                    display: block;
                                    width: 100%;
                                    padding: 0.875rem 0;
                                    font-size: 1rem;
                                    letter-spacing: 0.28em;
                                    text-transform: uppercase;
                                "
                            >
                                {{ submitting ? '送出中…' : '送出點餐' }}
                            </button>
                            <button
                                @click="clearAll"
                                class="clear-btn font-label"
                                style="
                                    display: block;
                                    width: 100%;
                                    padding: 0.625rem 0;
                                    font-size: 1rem;
                                    letter-spacing: 0.1em;
                                    text-transform: uppercase;
                                "
                            >
                                清空訂單
                            </button>
                        </div>
                    </div>
                </aside>
            </Teleport>
        </div>

        <!-- ── 手機底部列 ── -->
        <div class="mobile-bottom-bar" @click="orderPanelOpen = true">
            <div class="flex items-center gap-3">
                <span class="bottom-badge font-label">共 {{ store.totalItems }} 項</span>
            </div>
            <div class="flex items-center gap-3">
                <span
                    class="font-label text-sm tracking-wide"
                    style="color: #e3c76b; padding-right: 1rem"
                    >NT$ {{ finalTotal.toLocaleString() }}</span
                >
                <span class="bottom-cta font-label text-xs tracking-[0.2em] uppercase"
                    >送出點餐</span
                >
            </div>
        </div>

        <!-- 手機 overlay（Teleport 到 body，避免 root-wrap overflow:hidden 影響 z-index） -->
        <Teleport to="body">
            <div v-if="orderPanelOpen" @click="orderPanelOpen = false" class="mobile-overlay"></div>
        </Teleport>

        <!-- 優惠券 × 活動衝突警告 Modal -->
        <Teleport to="body">
            <Transition name="cew-fade">
                <div v-if="couponEventWarnModal" class="cew-overlay" @click.self="cancelCouponWarn">
                    <div class="cew-box">
                        <div class="cew-icon">⚠️</div>
                        <p class="font-headline cew-title">注意</p>
                        <p class="font-label cew-body">
                            使用此優惠券後，需要再消費
                            <strong class="cew-highlight">
                                NT$
                                {{
                                    (bestAutoEvent
                                        ? bestAutoEvent.minSpend -
                                          (total - (pendingCouponData?.discount ?? 0))
                                        : 0
                                    ).toLocaleString()
                                }}
                            </strong>
                            才可享「<strong class="cew-highlight">{{ bestAutoEvent?.title }}</strong
                            >」 活動優惠（{{ bestAutoEvent?.discountDescription }}）。
                        </p>
                        <p class="font-label cew-sub">確定要使用優惠券嗎？</p>
                        <div class="cew-btns">
                            <button class="cew-btn-cancel font-label" @click="cancelCouponWarn">
                                取消
                            </button>
                            <button
                                class="cew-btn-confirm font-label"
                                @click="confirmCouponDespiteEvent"
                            >
                                確定使用
                            </button>
                        </div>
                    </div>
                </div>
            </Transition>
        </Teleport>

        <!-- Notify Events Toast（IsAutoDiscount=0 活動，右下角通知） -->
        <Teleport to="body">
            <div class="notify-toast-stack">
                <TransitionGroup name="notify-toast">
                    <div
                        v-for="toast in notifyToasts"
                        :key="toast.key"
                        :class="[
                            'notify-toast-card',
                            toast.type === 'near'
                                ? 'near'
                                : toast.type === 'eligible-notify'
                                  ? 'eligible'
                                  : toast.type === 'one-event-note'
                                    ? 'info'
                                    : toast.type === 'applied-event'
                                      ? 'applied'
                                      : 'eligible',
                        ]"
                    >
                        <button class="notify-toast-close" @click="dismissToast(toast.key)">
                            ✕
                        </button>
                        <div class="notify-toast-icon">
                            {{
                                toast.type === 'near'
                                    ? '🔥'
                                    : toast.type === 'one-event-note'
                                      ? 'ℹ️'
                                      : toast.type === 'applied-event'
                                        ? '🎉'
                                        : '🎁'
                            }}
                        </div>
                        <p class="font-body notify-toast-msg">
                            <template v-if="toast.type === 'near'">
                                差 NT${{ toast.ev.minSpend - total }} 即可參加「{{
                                    toast.ev.title
                                }}」活動，享 {{ toast.ev.discountDescription }} 優惠！
                            </template>
                            <template v-else-if="toast.type === 'eligible-notify'">
                                <template v-if="!isLoggedIn">
                                    恭喜！金額已達活動門檻，<span
                                        @click="openAuthModal"
                                        style="
                                            color: #e3c76b;
                                            font-weight: 700;
                                            text-decoration: underline;
                                            cursor: pointer;
                                        "
                                        >登入會員</span
                                    >即可參加「{{ toast.ev.title }}」活動{{
                                        toast.ev.summary ? `(${toast.ev.summary})` : ''
                                    }}！
                                </template>
                                <template v-else>
                                    恭喜！金額已達門檻，可參加「{{ toast.ev.title }}」活動{{
                                        toast.ev.summary ? `(${toast.ev.summary})` : ''
                                    }}！（請洽現場服務人員）
                                </template>
                            </template>
                            <template v-else-if="toast.type === 'one-event-note'">
                                每次用餐能參加一個活動，不得與其他優惠活動合併使用
                            </template>
                            <template v-else-if="toast.type === 'applied-event'">
                                已參加「{{ toast.ev.title }}」活動 （滿 NT${{
                                    toast.ev.minSpend
                                }}
                                可享 {{ toast.ev.discountDescription }}）
                            </template>
                        </p>
                    </div>
                </TransitionGroup>
            </div>
        </Teleport>

        <!-- 選擇登入 or 訪客 Modal -->
        <OrderAuthModal v-if="authModalVisible" @login="openAuthModal" @guest="onGuestOrder" />

        <!-- Detail Modal -->
        <Teleport to="body">
            <DishDetailModal
                :dish="activeDetail"
                :edit-mode="editingLineId !== null"
                :initial-qty="
                    editingLineId !== null
                        ? (cartItemsWithDetails.find((i) => i.lineId === editingLineId)?.qty ?? 1)
                        : (activeDetail?.defaultQty ?? 1)
                "
                :initial-note="
                    editingLineId !== null
                        ? (cartItemsWithDetails.find((i) => i.lineId === editingLineId)?.note ?? '')
                        : ''
                "
                @close="((activeDetail = null), (editingLineId = null))"
                @confirm="onDetailConfirm"
            />
        </Teleport>

        <!-- 套餐選項 Modal -->
        <SetMealSelectModal
            :meal="activeMeal"
            :edit-mode="editingMealLineId !== null"
            :initial-qty="
                editingMealLineId !== null
                    ? (store.lines.find((l) => l.lineId === editingMealLineId)?.qty ?? 1)
                    : 1
            "
            :initial-note="
                editingMealLineId !== null
                    ? (store.lines.find((l) => l.lineId === editingMealLineId)?.note ?? '')
                    : ''
            "
            :initial-sel="
                editingMealLineId !== null ? buildInitialSel(editingMealLineId) : demoMealInitialSel
            "
            @close="((activeMeal = null), (editingMealLineId = null), (demoMealInitialSel = {}))"
            @confirm="onSetMealConfirm"
        />

        <!-- Success Modal -->
        <OrderSuccessModal
            :visible="successModalOpen"
            :order-number="orderNumber"
            :table-label="tables.find((t) => t.id === tableId)?.tableName || String(tableId)"
            @close="onSuccessClose"
        />

        <!-- Pax Error Alert -->
        <Teleport to="body">
            <Transition name="pax-alert">
                <div v-if="paxError" class="pax-alert-wrap">
                    <div class="pax-alert-box">
                        <span class="pax-alert-icon">⚠</span>
                        <div class="pax-alert-text">
                            <p class="pax-alert-title">請填寫用餐人數</p>
                            <p class="pax-alert-sub">點頂部「＋」設定人數後再送出</p>
                        </div>
                    </div>
                </div>
            </Transition>
        </Teleport>

        <!-- Toast -->
        <div
            class="toast-wrap"
            :style="
                toastVisible
                    ? 'opacity:1;transform:translateX(-50%) translateY(0)'
                    : 'opacity:0;pointer-events:none;transform:translateX(-50%) translateY(10px)'
            "
        >
            <div
                class="px-4 py-3 shadow-xl flex items-center gap-3"
                style="
                    background: #362620;
                    border: 1px solid rgba(77, 70, 58, 0.4);
                    width: min(340px, 88vw);
                "
            >
                <span style="color: #e3c76b">+</span>
                <span
                    class="font-label text-xs tracking-widest uppercase"
                    style="color: #f9ddd3; font-size: 1rem"
                    >{{ toastMsg }}</span
                >
            </div>
        </div>
    </div>
    <!-- end root-wrap -->
</template>

<script setup>
import { ref, reactive, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { useOrderStore } from '@/stores/order'
import { useAuthStore } from '@/stores/auth'
import apiFetch from '@/utils/apiFetch'
import DishDetailModal from '@/components/order/DishDetailModal.vue'
import SetMealSelectModal from '@/components/order/SetMealSelectModal.vue'
import OrderSuccessModal from '@/components/order/OrderSuccessModal.vue'
import OrderAuthModal from '@/components/order/OrderAuthModal.vue'

const route = useRoute()
const store = useOrderStore()
const authStore = useAuthStore()
const tableId = ref('')

const imgErrors = reactive(new Set()) // 圖片完全載入失敗的 productId
const imgFallback = reactive(new Map()) // productId → fallback URL（jpg→png）
const products = ref([])
const tables = ref([]) // 桌位清單
const loading = ref(true)
const loadError = ref('')
const searchQuery = ref('')
const activeChip = ref('all')
const activeSidebarCat = ref('')
const curView = ref('list')
const orderPanelOpen = ref(false)
const activeDetail = ref(null)
const editingLineId = ref(null) // null = 新增模式，有值 = 編輯模式

// ── 套餐選項 Modal ──────────────────────────────────────────────
const activeMeal = ref(null) // 目前開啟的套餐資料（API 回傳）
const editingMealLineId = ref(null) // 套餐編輯模式的 lineId
const demoMealInitialSel = ref({})
const successModalOpen = ref(false)
const orderNumber = ref('')
const submitting = ref(false)
const toastVisible = ref(false)
const toastMsg = ref('')
let toastTimer = null
const paxError = ref(false)
let paxErrorTimer = null

// ── 桌位狀態：'checking' | 'occupied' | 'empty' | 'reserved' | 'not-found'
// 若 URL 沒有 ?table= 參數則跳過驗證（讓服務人員可直接開頁面選桌）
const tableStatus = ref(route.query.table ? 'checking' : 'no-table')
const tableNameFromUrl = route.query.table || '' // e.g. "B3"

// ── 身份驗證（使用全域 authStore）
const memberDropdownOpen = ref(false)
const authModalVisible = ref(!authStore.isLoggedIn) // 已登入則直接跳過
const isLoggedIn = computed(() => authStore.isLoggedIn)
const memberName = computed(() => authStore.member?.name || '訪客')
const currentMemberId = computed(() => authStore.member?.id ?? null)

// ── 會員收藏
const favoriteProducts = ref([])

async function loadFavorites() {
    try {
        const res = await apiFetch('/Orders/Favorites')
        if (res.ok) favoriteProducts.value = await res.json()
    } catch {}
}

// 歷史訂單
const orderHistory = ref([])

async function loadOrderHistory() {
    try {
        const res = await apiFetch('/Orders/MemberOrderHistory')
        if (res.ok) orderHistory.value = await res.json()
    } catch {}
}

const couponCode = ref('')
const couponMsg = ref('')
const couponOk = ref(false)
const couponId = ref(null)
const couponDiscount = ref(0) // 實際折扣金額

// 優惠券 × 活動衝突警告 Modal
const couponEventWarnModal = ref(false)
const pendingCouponData = ref(null) // { discount, couponId, message }

// 已領優惠券下拉選單
const myCoupons = ref([])
const selectedCouponCode = ref('')

// 只顯示未使用且未過期的券
const availableCoupons = computed(() => myCoupons.value.filter((c) => !c.isUsed && !c.isExpired))

async function loadMyCoupons() {
    try {
        const res = await apiFetch('/Coupons/My')
        if (res.ok) myCoupons.value = await res.json()
    } catch {}
}

// 下拉選單選擇優惠券時自動套用；選到特殊項目時導頁
async function onCouponSelect() {
    if (selectedCouponCode.value === '__coupons__') {
        // 導到優惠券專區，同時把 select 重設避免殘留
        selectedCouponCode.value = ''
        window.location.href = '/coupons'
        return
    }
    if (!selectedCouponCode.value) {
        resetCoupon()
        return
    }
    couponCode.value = selectedCouponCode.value
    await applyCoupon()
}

function resetCoupon() {
    couponCode.value = ''
    couponMsg.value = ''
    couponOk.value = false
    couponId.value = null
    couponDiscount.value = 0
    selectedCouponCode.value = ''
}

function clearAll() {
    store.clearOrder()
    resetCoupon()
}

async function applyCoupon() {
    if (!couponCode.value.trim()) return
    couponOk.value = false
    couponId.value = null
    couponDiscount.value = 0
    couponMsg.value = ''
    try {
        const res = await apiFetch('/Orders/ValidateCoupon', {
            method: 'POST',
            body: JSON.stringify({
                code: couponCode.value.trim().toUpperCase(),
                orderAmount: total.value,
            }),
        })
        const data = await res.json()
        if (data.isValid) {
            const discount = data.discount ?? 0
            // 已套用活動且使用優惠券後金額低於活動門檻 → 先警告
            if (
                isLoggedIn.value &&
                bestAutoEvent.value &&
                total.value - discount < bestAutoEvent.value.minSpend
            ) {
                pendingCouponData.value = {
                    discount,
                    couponId: data.couponId,
                    message: data.message || `折抵 NT$ ${discount}`,
                }
                couponEventWarnModal.value = true
            } else {
                couponMsg.value = data.message || `折抵 NT$ ${discount}`
                couponOk.value = true
                couponId.value = data.couponId
                couponDiscount.value = discount
            }
        } else {
            couponMsg.value = data.message || '優惠券無效或不符條件'
        }
    } catch {
        couponMsg.value = '驗證失敗，請稍後再試'
    }
}

// 使用者確認「仍要用優惠券」
function confirmCouponDespiteEvent() {
    if (!pendingCouponData.value) return
    const { discount, couponId: cid, message } = pendingCouponData.value
    couponMsg.value = message
    couponOk.value = true
    couponId.value = cid
    couponDiscount.value = discount
    pendingCouponData.value = null
    couponEventWarnModal.value = false
}

// 使用者取消（不套用優惠券）
// 使用者取消（不套用優惠券）→ 還原選擇器至「選擇優惠券」
function cancelCouponWarn() {
    pendingCouponData.value = null
    couponEventWarnModal.value = false
    resetCoupon()
}

// ── 開啟全域登入 Modal（Bootstrap #authModal）
function openAuthModal() {
    memberDropdownOpen.value = false
    authModalVisible.value = false // 先關閉選擇 Modal
    const modalEl = document.querySelector('#authModal')
    if (modalEl) {
        import('bootstrap').then(({ Modal }) => {
            Modal.getOrCreateInstance(modalEl).show()
        })
    }
}

// ── 以訪客繼續（關閉選擇 Modal 即可）
function onGuestOrder() {
    authModalVisible.value = false
}

// ── 登入狀態變化時，自動載入收藏 / 歷史訂單
watch(isLoggedIn, (loggedIn) => {
    if (loggedIn) {
        authModalVisible.value = false // 登入成功 → 關閉選擇 Modal
        loadFavorites()
        loadOrderHistory()
        loadMyCoupons()
        // 登入後重新 fetch：清掉訪客 toast、改以會員身分計算活動
        if (total.value > 0) fetchActiveEvents()
    } else {
        favoriteProducts.value = []
        orderHistory.value = []
        myCoupons.value = []
        resetCoupon()
    }
})

// ── 此頁為全螢幕版面，移除全域 body padding（Navbar 已隱藏）──
onMounted(async () => {
    document.body.style.paddingTop = '0'

    // 路由守衛已 await checkAuth()，此時 isLoggedIn 已確定
    // watch(isLoggedIn) 不會再次觸發（無值變化），需在此明確呼叫
    if (isLoggedIn.value) {
        loadFavorites()
        loadOrderHistory()
        loadMyCoupons()
    }

    try {
        const [menuRes, tablesRes] = await Promise.all([
            apiFetch('/Orders/MenuItems'),
            apiFetch('/Orders/Tables'),
        ])
        if (!menuRes.ok) throw new Error(`HTTP ${menuRes.status}`)
        products.value = await menuRes.json()
        if (tablesRes.ok) tables.value = await tablesRes.json()

        // ── 桌位狀態驗證（只在有 ?table= 時執行）──
        if (tableNameFromUrl) {
            const matched = tables.value.find(
                (t) => t.tableName?.toLowerCase() === tableNameFromUrl.toLowerCase()
            )
            if (!matched) {
                tableStatus.value = 'not-found'
            } else if (matched.status === 1) {
                tableStatus.value = 'occupied'
                tableId.value = matched.id // 自動帶入桌號
            } else if (matched.status === 2) {
                tableStatus.value = 'reserved'
            } else {
                tableStatus.value = 'empty' // 0 或其他皆視為空桌
            }
        }

        // 預設選第一個 sidebar 分類（優先今日推薦，否則第一個一般分類）
        activeSidebarCat.value = sidebarCategories.value[0]?.key ?? ''
    } catch (err) {
        loadError.value = '菜單載入失敗，請稍後再試。'
        tableStatus.value = 'not-found' // API 失敗時不讓頁面永遠卡在 checking
        console.error(err)
    } finally {
        loading.value = false
    }
})

onUnmounted(() => {
    document.body.style.paddingTop = '' // 離開頁面時還原
})

function handleImgError(dish) {
    const current = imgFallback.get(dish.productId) || resolveImage(dish.imageUrl)
    if (/\.jpg$/i.test(current)) {
        // jpg 失敗 → 先試 png
        imgFallback.set(dish.productId, current.replace(/\.jpg$/i, '.png'))
    } else {
        // png 也失敗（或本來就不是 jpg）→ 顯示 placeholder
        imgErrors.add(dish.productId)
    }
}

function resolveImage(url) {
    if (!url) return ''
    return url
}

// ── 展示用分類（從真實 products 過濾，保留真實 productId / setMealId / 價格）──
const DEMO_CATEGORY_KEY = '展示'
const DEMO_SPECS = [
    { name: '香烤雞腿排', defaultQty: 1 },
    { name: '全家分享餐', defaultQty: 1 },
    { name: '奶油培根燉飯', defaultQty: 3 },
    { name: '瑪格麗特披薩', defaultQty: 1 },
]
const demoDishes = computed(() =>
    DEMO_SPECS.map(({ name, defaultQty }) => {
        const p = products.value.find((p) => p.productName === name)
        return p ? { ...p, defaultQty } : null
    }).filter(Boolean)
)

// 一般分類排序（特殊的今日推薦/主廚特選不在此列）
const CATEGORY_ORDER = ['套餐', '主餐', '湯品', '甜點', '附餐', '飲料']

const sidebarCategories = computed(() => {
    // ── 特殊分類（今日推薦/主廚特選依資料有無顯示；會員專區登入即顯示）──
    const specials = [
        {
            key: '今日推薦',
            label: '今日推薦',
            count: products.value.filter((p) => p.isRecommended).length,
        },
        {
            key: '主廚特選',
            label: '主廚特選',
            count: products.value.filter((p) => p.isPopular).length,
        },
    ].filter((s) => s.count > 0)

    // 我的收藏 / 歷史訂單：資料非同步載入，不能用 count > 0 過濾，
    // 否則第一次計算時 count 為 0 會被永遠過濾掉
    if (isLoggedIn.value) {
        specials.push(
            { key: '我的收藏', label: '我的收藏', count: favoriteProducts.value.length },
            { key: '歷史訂單', label: '歷史訂單', count: orderHistory.value.length }
        )
    }

    // ── 一般分類 ──
    const map = new Map()
    products.value.forEach((p) => {
        const cat = p.categoryName
        if (!cat) return
        if (!map.has(cat)) map.set(cat, { key: cat, label: cat, count: 0 })
        map.get(cat).count++
    })
    const cats = Array.from(map.values()).sort((a, b) => {
        const ai = CATEGORY_ORDER.indexOf(a.key),
            bi = CATEGORY_ORDER.indexOf(b.key)
        return (ai < 0 ? 99 : ai) - (bi < 0 ? 99 : bi)
    })

    // 全部：放在分隔線後、一般分類前
    const allEntry = { key: '全部', label: '全部', count: products.value.length }

    return [
        ...specials,
        allEntry,
        ...cats,
        {
            key: DEMO_CATEGORY_KEY,
            label: DEMO_CATEGORY_KEY,
            count: demoDishes.value.length,
            isDividerBefore: true,
        },
    ]
})

const filteredProducts = computed(() => {
    let r = products.value
    if (searchQuery.value) {
        const t = searchQuery.value.toLowerCase()
        r = r.filter((p) => p.productName?.toLowerCase().includes(t))
    }
    // 只剩素食/辣味 chip 篩選
    if (activeChip.value === 'veg') r = r.filter((p) => p.isVegetarian)
    if (activeChip.value === 'spicy') r = r.filter((p) => p.spicyLevel > 0)
    return r
})

const displaySections = computed(() => {
    // 收藏分類
    if (activeSidebarCat.value === '我的收藏') {
        if (!favoriteProducts.value.length) return []
        return [{ key: '我的收藏', label: '我的收藏', dishes: favoriteProducts.value }]
    }
    // 再點一次
    if (activeSidebarCat.value === '歷史訂單') {
        if (!orderHistory.value.length) return []
        return [{ key: '歷史訂單', label: '歷史訂單', dishes: [], orders: orderHistory.value }]
    }
    // 搜尋模式：跨所有分類
    if (searchQuery.value.trim()) {
        const dishes = filteredProducts.value
        if (!dishes.length) return []
        return [{ key: 'search', label: `搜尋：${searchQuery.value}`, dishes }]
    }
    // 特殊分類
    if (activeSidebarCat.value === '今日推薦') {
        const dishes = filteredProducts.value.filter((p) => p.isRecommended)
        if (!dishes.length) return []
        return [{ key: '今日推薦', label: '今日推薦', dishes }]
    }
    if (activeSidebarCat.value === '主廚特選') {
        const dishes = filteredProducts.value.filter((p) => p.isPopular)
        if (!dishes.length) return []
        return [{ key: '主廚特選', label: '主廚特選', dishes }]
    }
    // 全部：按分類順序分組顯示
    if (activeSidebarCat.value === '全部') {
        return CATEGORY_ORDER.map((cat) => ({
            key: cat,
            label: cat,
            dishes: filteredProducts.value.filter((p) => p.categoryName === cat),
        })).filter((s) => s.dishes.length > 0)
    }
    // 展示分類
    if (activeSidebarCat.value === DEMO_CATEGORY_KEY) {
        return [{ key: DEMO_CATEGORY_KEY, label: DEMO_CATEGORY_KEY, dishes: demoDishes.value }]
    }
    // 一般分類
    const dishes = filteredProducts.value.filter((p) => p.categoryName === activeSidebarCat.value)
    if (!dishes.length) return []
    return [{ key: activeSidebarCat.value, label: activeSidebarCat.value, dishes }]
})

const cartItemsWithDetails = computed(() =>
    store.lines
        .map((line) => {
            if (line.isSetMeal) {
                // 套餐 line 直接用儲存的資料
                return {
                    lineId: line.lineId,
                    productId: line.productId,
                    productName: line.setMealData?.name ?? '套餐',
                    unitPrice: line.unitPrice ?? 0,
                    qty: line.qty,
                    note: line.note,
                    isSetMeal: true,
                    setMealData: line.setMealData,
                }
            }
            const d = products.value.find((p) => p.productId === line.productId)
            return d ? { ...d, lineId: line.lineId, qty: line.qty, note: line.note } : null
        })
        .filter(Boolean)
)

const total = computed(() =>
    cartItemsWithDetails.value.reduce((s, i) => s + i.unitPrice * i.qty, 0)
)

// ── 活動 ──────────────────────────────────────────────────────────
const autoEvents = ref([]) // IsAutoDiscount=1，已達門檻
const notifyEvents = ref([]) // IsAutoDiscount=0，通知型
const nearAutoEvents = ref([]) // IsAutoDiscount=1，差額≤100

// ── Gift 贈品（Gift 型活動自動加入購物車）──
const giftCartItem = ref(null) // { productId, productName, qty: 1 } | null

// ── Notify Toast（右下角通知）──
const notifyToasts = ref([]) // [{ key, ev, persistent?, type? }]
let _toastKeySeq = 0

// 各類 toast 的 key 追蹤（id → key），每次 fetch 做增刪比對
const _nearToastKeys = new Map() // ev.id → key（差額提示）
const _eligibleNotifyKeys = new Map() // ev.id → key（IsAutoDiscount=0 達門檻）
let _oneEventNoteKey = null // 「一個活動限制」提示 key
let _lastBestEventId = null // 上次 bestAutoEvent 的 id（避免重複推 toast）
const _guestAutoKeys = new Map() // ev.id → key（訪客 autoEvents 達門檻通知）
let _appliedEventToastTimer = null // applied-event toast 自動消失計時器

function dismissToast(key) {
    notifyToasts.value = notifyToasts.value.filter((t) => t.key !== key)
}
function pushToast(ev, opts = {}) {
    const key = ++_toastKeySeq
    notifyToasts.value.push({ key, ev: { ...ev }, ...opts })
    return key
}

// ── 最佳自動活動：MinSpend 最高，同門檻取折扣最大 ──
const bestAutoEvent = computed(() => {
    if (!autoEvents.value.length) return null
    return [...autoEvents.value].sort((a, b) =>
        b.minSpend !== a.minSpend
            ? b.minSpend - a.minSpend
            : (b.calculatedDiscount ?? 0) - (a.calculatedDiscount ?? 0)
    )[0]
})

const autoEventDiscount = computed(() =>
    isLoggedIn.value && bestAutoEvent.value ? (bestAutoEvent.value.calculatedDiscount ?? 0) : 0
)

async function fetchActiveEvents(amount = null) {
    const effectiveAmount = amount ?? total.value
    try {
        const res = await apiFetch(`/Orders/ActiveEvents?amount=${effectiveAmount}`)
        if (!res.ok) return
        const data = await res.json()
        autoEvents.value = data.autoEvents ?? []
        notifyEvents.value = data.notifyEvents ?? []
        nearAutoEvents.value = data.nearAutoEvents ?? []

        const autoIdSet = new Set(autoEvents.value.map((e) => e.id))
        const nearAutoIdSet = new Set(nearAutoEvents.value.map((e) => e.id))
        const hasBest = !!bestAutoEvent.value
        const bestId = bestAutoEvent.value?.id

        // ── 所有已達門檻的活動（auto + notify eligible），按 minSpend 降序排列
        // overallBestId = 門檻最高的那個，只有它才顯示「過門檻」eligible-notify toast
        const allEligibleSorted = [
            ...autoEvents.value,
            ...notifyEvents.value.filter((e) => e.isEligible),
        ].sort((a, b) => b.minSpend - a.minSpend)
        const overallBestId = allEligibleSorted[0]?.id ?? null

        // 有 ≥ 2 個活動達標 → 需要顯示「不得合併使用」警語
        const hasMultipleEligible = allEligibleSorted.length > 1

        // ── 清除：訪客 autoEvent 的 eligible-notify，只保留 overallBest ──
        for (const [id, key] of _guestAutoKeys) {
            if (!autoIdSet.has(id) || isLoggedIn.value || id !== overallBestId) {
                dismissToast(key)
                _guestAutoKeys.delete(id)
            }
        }

        // ── 清除：notifyEvent 的 eligible-notify，只保留 overallBest ──
        for (const [id, key] of _eligibleNotifyKeys) {
            const stillEligible = notifyEvents.value.some((e) => e.id === id && e.isEligible)
            if (!stillEligible || id !== overallBestId) {
                dismissToast(key)
                _eligibleNotifyKeys.delete(id)
            }
        }

        // ── 清除失效的差額 toast（事件不再差額≤100）──
        for (const [id, key] of _nearToastKeys) {
            const stillNearAuto = nearAutoIdSet.has(id)
            const stillNearNotify = notifyEvents.value.some(
                (e) =>
                    e.id === id &&
                    !e.isEligible &&
                    e.minSpend - total.value > 0 &&
                    e.minSpend - total.value <= 100
            )
            if (!stillNearAuto && !stillNearNotify) {
                dismissToast(key)
                _nearToastKeys.delete(id)
            }
        }

        // ── 清除 one-event-note：無達標活動或未滿 2 個達標時移除 ──
        if (_oneEventNoteKey !== null && (!overallBestId || !hasMultipleEligible)) {
            dismissToast(_oneEventNoteKey)
            _oneEventNoteKey = null
        }

        // ── 0. 訪客：只顯示 overallBest 的 eligible-notify（若為 autoEvent）──
        if (
            !isLoggedIn.value &&
            hasBest &&
            bestId === overallBestId &&
            !_guestAutoKeys.has(bestId)
        ) {
            const key = pushToast(bestAutoEvent.value, {
                persistent: true,
                type: 'eligible-notify',
            })
            _guestAutoKeys.set(bestId, key)
        }

        // ── 1. nearAutoEvents（差額≤100）→ 差額 toast ──
        nearAutoEvents.value.forEach((ev) => {
            if (!_nearToastKeys.has(ev.id)) {
                _nearToastKeys.set(ev.id, pushToast(ev, { persistent: true, type: 'near' }))
            }
        })

        // ── 2. notifyEvents（IsAutoDiscount=0）──
        notifyEvents.value.forEach((ev) => {
            const gap = ev.minSpend - total.value
            if (ev.isEligible) {
                // 已達門檻 → 清差額 toast
                const nearKey = _nearToastKeys.get(ev.id)
                if (nearKey !== undefined) {
                    dismissToast(nearKey)
                    _nearToastKeys.delete(ev.id)
                }
                // 只有 overallBest 才顯示 eligible-notify，避免多個達標同時顯示
                if (ev.id === overallBestId && !_eligibleNotifyKeys.has(ev.id)) {
                    _eligibleNotifyKeys.set(
                        ev.id,
                        pushToast(ev, { persistent: true, type: 'eligible-notify' })
                    )
                }
            } else if (gap > 0 && gap <= 100 && !_nearToastKeys.has(ev.id)) {
                // 差額 > 0 且 ≤ 100 → 差額 toast（防止負值）
                _nearToastKeys.set(ev.id, pushToast(ev, { persistent: true, type: 'near' }))
            }
        })

        // ── 3. 有 ≥ 2 個活動達標 → 「每次用餐只能參加一個活動」警語 ──
        if (overallBestId && hasMultipleEligible && _oneEventNoteKey === null) {
            _oneEventNoteKey = pushToast(
                { id: -1, title: '', discountDescription: '' },
                { persistent: true, type: 'one-event-note' }
            )
        }
    } catch {
        /* 靜默失敗 */
    }
}

watch(total, (val) => {
    if (val > 0) {
        fetchActiveEvents(val - couponDiscount.value)
    } else {
        autoEvents.value = []
        notifyEvents.value = []
        nearAutoEvents.value = []
        notifyToasts.value = []
        _nearToastKeys.clear()
        _eligibleNotifyKeys.clear()
        _guestAutoKeys.clear()
        _oneEventNoteKey = null
        giftCartItem.value = null
        _lastBestEventId = null
        clearTimeout(_appliedEventToastTimer)
    }
})

// 套用 / 清除優惠券時，以扣券後的有效金額重新判斷活動門檻
// 確保優惠券折扣使金額低於門檻時，差額 toast 能正確顯示
watch(couponDiscount, (discount) => {
    if (total.value > 0) {
        fetchActiveEvents(total.value - discount)
    }
})

// ── bestAutoEvent 切換：更新贈品 + 顯示 3 秒 applied-event toast ──
watch(bestAutoEvent, (newEv) => {
    // 1. 贈品更新
    giftCartItem.value = null
    if (isLoggedIn.value && newEv?.discountType === 'Gift' && newEv.rewardDishId) {
        const dish = products.value.find((p) => p.productId === newEv.rewardDishId)
        if (dish) {
            giftCartItem.value = {
                productId: dish.productId,
                productName: dish.productName,
                qty: 1,
            }
        }
    }

    // 2. 事件 id 真正改變才推 toast（含 null→有、有→null、有→不同有）
    const newId = newEv?.id ?? null
    if (newId !== _lastBestEventId) {
        _lastBestEventId = newId
        if (newEv && isLoggedIn.value) {
            clearTimeout(_appliedEventToastTimer)
            const key = pushToast(newEv, { persistent: false, type: 'applied-event' })
            _appliedEventToastTimer = setTimeout(() => dismissToast(key), 2000)
        }
    }
})

const finalTotal = computed(() =>
    Math.max(0, total.value - couponDiscount.value - autoEventDiscount.value)
)

function handleAdd(dish) {
    store.addItem(dish.productId)
    showToast(`「${dish.productName}」已加入訂單`)
}
function handleRemove(dish) {
    store.removeItem(dish.productId)
}
// 素食/辣味 chip 點同一個再點可取消
function toggleChip(c) {
    activeChip.value = activeChip.value === c ? 'all' : c
}

function scrollToSection(cat) {
    activeSidebarCat.value = cat
    searchQuery.value = ''
    // 切換分類後捲回頂端
    document.getElementById('menuMain')?.scrollTo({ top: 0, behavior: 'smooth' })
}

async function openDetail(dish) {
    if (dish.isSetMeal && dish.setMealId) {
        // 套餐 → 開套餐選項 Modal
        editingMealLineId.value = null
        try {
            const res = await apiFetch(`/SetMeals/${dish.setMealId}`)
            if (res.ok) {
                const meal = await res.json()
                activeMeal.value = meal
                // 展示用套餐：內用從尾選
                const isDemo = demoDishes.value.some((d) => d.productId === dish.productId)
                demoMealInitialSel.value = isDemo ? buildDemoInitialSel(meal, true) : {}
            }
        } catch {
            /* 靜默 */
        }
        return
    }
    editingLineId.value = null
    activeDetail.value = dish
}

/** 購物車列點擊 → 編輯模式 */
async function openCartItemEdit(item) {
    if (item.isSetMeal) {
        // 套餐：重新打開套餐選項 Modal（帶入舊選項）
        editingMealLineId.value = item.lineId
        const smId = item.setMealData?.id
        if (!smId) return
        try {
            const res = await apiFetch(`/SetMeals/${smId}`)
            if (res.ok) activeMeal.value = await res.json()
        } catch {
            /* 靜默 */
        }
        return
    }
    editingLineId.value = item.lineId
    activeDetail.value = products.value.find((p) => p.productId === item.productId) ?? item
}

function onDetailConfirm(dish, qty, note) {
    if (editingLineId.value !== null) {
        // ── 編輯模式：先完整刪除舊 line，再以新 qty/note 加入（自動合併同名 line）──
        store.deleteLine(editingLineId.value)
        for (let i = 0; i < qty; i++) store.addItem(dish.productId, note || '')
        showToast(`「${dish.productName}」已更新`)
        editingLineId.value = null
    } else {
        // ── 新增模式 ──
        for (let i = 0; i < qty; i++) store.addItem(dish.productId, note || '')
        showToast(`「${dish.productName}」× ${qty} 已加入訂單`)
    }
    activeDetail.value = null
}

/** 編輯套餐時，把已選項目重建為 { [groupNo]: { [dishId]: qty } } */
function buildInitialSel(lineId) {
    const line = store.lines.find((l) => l.lineId === lineId)
    if (!line?.setMealData?.selectedOptions) return {}
    const sel = {}
    for (const opt of line.setMealData.selectedOptions) {
        // groupNo 要從 activeMeal items 反查（目前 activeMeal 可能還沒載入）
        // 先存在 dishId key 下，SetMealSelectModal 的 watch 會在 meal 載入後處理
        sel[opt.groupNo ?? 0] = sel[opt.groupNo ?? 0] ?? {}
        sel[opt.groupNo ?? 0][opt.dishId] = opt.qty
    }
    return sel
}

// 展示用套餐預選（TakeOut 從頭選，DineIn 從尾選）
function buildDemoInitialSel(meal, fromEnd = false) {
    const groupMap = {}
    for (const item of meal.items ?? []) {
        if (!item.isOptional) continue
        const gno = item.optionGroupNo
        if (!groupMap[gno]) groupMap[gno] = { groupNo: gno, pickLimit: item.pickLimit, options: [] }
        groupMap[gno].options.push(item)
    }
    const sel = {}
    for (const group of Object.values(groupMap)) {
        const limit = group.pickLimit ?? 1
        const opts = fromEnd ? [...group.options].reverse() : group.options
        let picked = 0
        sel[group.groupNo] = {}
        for (const opt of opts) {
            if (picked >= limit) break
            sel[group.groupNo][opt.dishId] = 1
            picked++
        }
    }
    return sel
}

/** 套餐 Modal 確認 */
function onSetMealConfirm(meal, qty, note, selectedOptions) {
    const setMealData = {
        id: meal.id,
        name: meal.setMealName,
        fixedItems: (meal.items ?? []).filter((i) => !i.isOptional),
        selectedOptions,
    }
    if (editingMealLineId.value !== null) {
        // 編輯模式：更新既有 line
        store.updateSetMealLine(editingMealLineId.value, meal.setPrice, note, setMealData)
        showToast(`「${meal.setMealName}」已更新`)
        editingMealLineId.value = null
    } else {
        // 新增：每份各加一條 line
        for (let i = 0; i < qty; i++) {
            store.addSetMeal(
                products.value.find((p) => p.setMealId === meal.id && p.isSetMeal)?.productId ?? 0,
                meal.setPrice,
                note,
                setMealData
            )
        }
        showToast(`「${meal.setMealName}」× ${qty} 已加入訂單`)
    }
    activeMeal.value = null
}

async function submitOrder() {
    if (store.totalItems === 0 || submitting.value) return
    if (!tableId.value) {
        showToast('請先輸入桌號')
        return
    }
    if (!store.pax || store.pax < 1) {
        showPaxError()
        return
    }
    submitting.value = true
    try {
        const body = {
            tableId: Number(tableId.value),
            inOrOut: true,
            peopleNum: store.pax,
            isAddOrder: false,
            payMethod: 'Cash',
            note: store.specialRequest || null,
            memberId: currentMemberId.value,
            couponId: couponId.value,
            eventId: isLoggedIn.value && bestAutoEvent.value ? bestAutoEvent.value.id : null,
            discountAmount: (couponOk.value ? couponDiscount.value : 0) + autoEventDiscount.value,
            items: (() => {
                const result = []
                for (const i of cartItemsWithDetails.value) {
                    if (i.isSetMeal) {
                        // 套餐：parent + 固定 + 選填 全部展開，子項以 parentIndex 連結
                        const parentIdx = result.length
                        result.push({
                            productId: i.productId,
                            productName: i.productName,
                            qty: i.qty,
                            unitPrice: i.unitPrice,
                            isSetMeal: true,
                            note: i.note || null,
                            parentIndex: null,
                        })
                        for (const f of i.setMealData?.fixedItems ?? []) {
                            for (let q = 0; q < (f.quantity || 1); q++) {
                                result.push({
                                    productId: f.dishId ?? 0,
                                    productName: f.dishName,
                                    qty: 1,
                                    unitPrice: 0,
                                    isSetMeal: true,
                                    parentIndex: parentIdx,
                                })
                            }
                        }
                        for (const s of i.setMealData?.selectedOptions ?? []) {
                            for (let q = 0; q < (s.qty || 1); q++) {
                                result.push({
                                    productId: s.dishId ?? 0,
                                    productName: s.dishName,
                                    qty: 1,
                                    unitPrice: 0,
                                    isSetMeal: true,
                                    parentIndex: parentIdx,
                                })
                            }
                        }
                    } else {
                        result.push({
                            productId: i.productId,
                            productName: i.productName,
                            qty: i.qty,
                            unitPrice: i.unitPrice,
                            isSetMeal: false,
                            note: i.note || null,
                            parentIndex: null,
                        })
                    }
                }
                // Gift 贈品
                if (isLoggedIn.value && giftCartItem.value) {
                    result.push({
                        productId: giftCartItem.value.productId,
                        productName: giftCartItem.value.productName,
                        qty: 1,
                        unitPrice: 0,
                        isSetMeal: false,
                        note: '贈品',
                        parentIndex: null,
                    })
                }
                return result
            })(),
        }
        const res = await apiFetch('/Orders/CreatePreOrder', {
            method: 'POST',
            body: JSON.stringify(body),
        })
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        const data = await res.json()
        orderNumber.value = data.orderNumber
        store.clearOrder() // 送出成功立即清空購物車與備註
        resetCoupon()
        successModalOpen.value = true
        orderPanelOpen.value = false
    } catch (err) {
        console.error(err)
        showToast('送出失敗，請稍後再試')
    } finally {
        submitting.value = false
    }
}

function onSuccessClose() {
    successModalOpen.value = false
}

function showToast(msg) {
    toastMsg.value = msg
    toastVisible.value = true
    clearTimeout(toastTimer)
    toastTimer = setTimeout(() => {
        toastVisible.value = false
    }, 2500)
}

function showPaxError() {
    paxError.value = true
    clearTimeout(paxErrorTimer)
    paxErrorTimer = setTimeout(() => {
        paxError.value = false
    }, 2800)
}

async function reorder(order) {
    for (const item of order.items ?? []) {
        const matched = products.value.find((p) => p.productName === item.productName)
        if (!matched) continue

        if (item.isSetMeal && matched.isSetMeal && matched.setMealId) {
            // 套餐：重新抓套餐定義，重建 fixedItems 與 selectedOptions
            let fixedItems = []
            let selectedOptions = []
            try {
                const res = await apiFetch(`/SetMeals/${matched.setMealId}`)
                if (res.ok) {
                    const meal = await res.json()
                    fixedItems = (meal.items ?? []).filter((i) => !i.isOptional)
                    const optionalItems = (meal.items ?? []).filter((i) => i.isOptional)
                    for (const sub of item.subItems ?? []) {
                        const opt = optionalItems.find((o) => o.dishName === sub.productName)
                        if (opt) {
                            selectedOptions.push({
                                dishId: opt.dishId,
                                dishName: opt.dishName,
                                qty: sub.qty,
                                groupNo: opt.optionGroupNo,
                            })
                        }
                    }
                }
            } catch {
                /* fallback：留空，使用者可手動編輯 */
            }
            for (let i = 0; i < item.qty; i++) {
                store.addSetMeal(matched.productId, matched.unitPrice, item.note || '', {
                    id: matched.setMealId,
                    name: matched.productName,
                    fixedItems,
                    selectedOptions,
                })
            }
        } else {
            for (let i = 0; i < item.qty; i++) {
                store.addItem(matched.productId, item.note || '')
            }
        }
    }
    activeSidebarCat.value = '全部'
    showToast('已加入購物車')
}
</script>

<style>
/*
 * ╔══════════════════════════════════════════════════════════════╗
 * ║                  DineIn.vue  CSS 架構說明                   ║
 * ╠══════════════════════════════════════════════════════════════╣
 * ║  ----網頁版----   @media (min-width: 1101px)                ║
 * ║    → layout、餐點卡片、按鈕尺寸全部在此區                  ║
 * ║    → 改動僅影響網頁版，不影響手機板                         ║
 * ║                                                              ║
 * ║  ----手機板----   @media (max-width: 1100px)                ║
 * ║    → 獨立完整定義，不依賴任何「共用基底」                   ║
 * ║    → 第一個 block：版面結構（header / layout / panel）      ║
 * ║    → 第二個 block（底部）：餐點卡片完整樣式                 ║
 * ╚══════════════════════════════════════════════════════════════╝
 */

/* 內用點餐頁面：清除全域 padding，鎖死頁面高度（不讓 body 出捲軸）*/
html:has(.root-wrap),
body:has(.root-wrap) {
    padding-top: 0 !important;
    overflow: hidden !important;
    height: 100% !important;
}
/* 內用頁面隱藏全站 Navbar / Footer（掃碼點餐不需要） */
html:has(.root-wrap) nav.navbar,
html:has(.root-wrap) footer,
html:has(.gate-wrap) nav.navbar,
html:has(.gate-wrap) footer {
    display: none !important;
}

/* ═══ Pax Error Alert（Teleport to body，必須全域） ═══ */
.pax-alert-wrap {
    position: fixed;
    top: 5rem;
    left: 50%;
    transform: translateX(-50%);
    z-index: 9999;
    pointer-events: none;
}
.pax-alert-box {
    display: flex;
    align-items: center;
    gap: 0.9rem;
    padding: 0.95rem 1.5rem;
    background: #1f1007;
    border: 2px solid #ffb347;
    border-radius: 0.65rem;
    box-shadow:
        0 0 0 4px rgba(255, 179, 71, 0.12),
        0 12px 40px rgba(0, 0, 0, 0.7);
    white-space: nowrap;
}
.pax-alert-icon {
    font-size: 1.75rem;
    line-height: 1;
    color: #ffb347;
    flex-shrink: 0;
    animation: pax-icon-pop 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
}
@keyframes pax-icon-pop {
    0% {
        transform: scale(0.4) rotate(-15deg);
        opacity: 0;
    }
    100% {
        transform: scale(1) rotate(0deg);
        opacity: 1;
    }
}
.pax-alert-text {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
}
.pax-alert-title {
    font-size: 1rem;
    font-style: italic;
    font-weight: 600;
    color: #ffb347;
    margin: 0;
    letter-spacing: 0.05em;
}
.pax-alert-sub {
    font-size: 0.7rem;
    letter-spacing: 0.1em;
    color: rgba(249, 221, 211, 0.5);
    margin: 0;
}

/* Transition */
.pax-alert-enter-active {
    transition: all 0.28s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.pax-alert-leave-active {
    transition: all 0.35s ease;
}
.pax-alert-enter-from {
    opacity: 0;
    transform: translateX(-50%) translateY(-16px) scale(0.88);
}
.pax-alert-leave-to {
    opacity: 0;
    transform: translateX(-50%) translateY(-8px) scale(0.95);
}

/* ── 優惠券 × 活動衝突警告 Modal ── */
.cew-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.65);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9500;
}
.cew-box {
    background: #2a1a12;
    border: 1px solid rgba(227, 199, 107, 0.3);
    border-radius: 0.75rem;
    padding: 2rem 1.75rem 1.5rem;
    max-width: 360px;
    width: 90%;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5);
}
.cew-icon {
    font-size: 2rem;
    line-height: 1;
}
.cew-title {
    color: #e3c76b;
    font-size: 1.15rem;
    margin: 0;
}
.cew-body {
    color: rgba(208, 197, 181, 0.85);
    font-size: 0.9rem;
    line-height: 1.65;
    text-align: center;
    margin: 0;
}
.cew-highlight {
    color: #e3c76b;
    font-weight: 700;
}
.cew-sub {
    color: rgba(208, 197, 181, 0.55);
    font-size: 0.82rem;
    margin: 0;
}
.cew-btns {
    display: flex;
    gap: 0.75rem;
    width: 100%;
    margin-top: 0.25rem;
}
.cew-btn-cancel {
    flex: 1;
    padding: 0.65rem;
    border: 1px solid rgba(77, 70, 58, 0.5);
    border-radius: 0.35rem;
    background: transparent;
    color: rgba(208, 197, 181, 0.6);
    font-size: 0.9rem;
    cursor: pointer;
    transition:
        border-color 0.2s,
        color 0.2s;
}
.cew-btn-cancel:hover {
    border-color: rgba(208, 197, 181, 0.4);
    color: rgba(208, 197, 181, 0.9);
}
.cew-btn-confirm {
    flex: 1;
    padding: 0.65rem;
    border: none;
    border-radius: 0.35rem;
    background: linear-gradient(135deg, #e3c76b, #c6ab53);
    color: #3b2f00;
    font-size: 0.9rem;
    font-weight: 700;
    cursor: pointer;
    transition: opacity 0.2s;
}
.cew-btn-confirm:hover {
    opacity: 0.88;
}
.cew-fade-enter-active,
.cew-fade-leave-active {
    transition: opacity 0.2s;
}
.cew-fade-enter-from,
.cew-fade-leave-to {
    opacity: 0;
}

/* ═══ Notify Toast（右下角堆疊通知，Teleport to body） ═══ */
.notify-toast-stack {
    position: fixed;
    bottom: 1.5rem;
    right: 1.25rem;
    z-index: 8500;
    display: flex;
    flex-direction: column-reverse;
    gap: 0.6rem;
    max-width: 300px;
    pointer-events: none;
}
.notify-toast-card {
    position: relative;
    border-radius: 0.6rem;
    padding: 0.75rem 2.2rem 0.75rem 1rem;
    display: flex;
    align-items: flex-start;
    gap: 0.55rem;
    box-shadow: 0 6px 24px rgba(0, 0, 0, 0.45);
    pointer-events: all;
}
.notify-toast-card.eligible {
    background: #1e2d1a;
    border: 1px solid rgba(163, 217, 119, 0.45);
    border-left: 3px solid #a3d977;
}
.notify-toast-card.near {
    background: #2a1e10;
    border: 1px solid rgba(255, 160, 80, 0.45);
    border-left: 3px solid #ff9f4a;
}
.notify-toast-card.info {
    background: #141c25;
    border: 1px solid rgba(130, 170, 220, 0.35);
    border-left: 3px solid #82aadc;
}
.notify-toast-card.info .notify-toast-msg {
    color: rgba(180, 210, 240, 0.8);
}
.notify-toast-card.applied {
    background: #221a0e;
    border: 1px solid rgba(227, 199, 107, 0.45);
    border-left: 3px solid #e3c76b;
}
.notify-toast-card.applied .notify-toast-msg {
    color: #f5dfa0;
}
.notify-toast-icon {
    font-size: 1.1rem;
    flex-shrink: 0;
    margin-top: 0.05rem;
}
.notify-toast-msg {
    font-size: 0.78rem;
    line-height: 1.5;
    margin: 0;
    color: #f9ddd3;
}
.notify-toast-card.eligible .notify-toast-msg {
    color: #c4eda0;
}
.notify-toast-card.near .notify-toast-msg {
    color: #ffd0a0;
}
.notify-toast-close {
    position: absolute;
    top: 0.4rem;
    right: 0.5rem;
    background: none;
    border: none;
    color: rgba(208, 197, 181, 0.35);
    font-size: 0.7rem;
    cursor: pointer;
    padding: 0.15rem 0.3rem;
    transition: color 0.2s;
    line-height: 1;
}
.notify-toast-close:hover {
    color: rgba(208, 197, 181, 0.8);
}

/* TransitionGroup */
.notify-toast-enter-active {
    transition:
        opacity 0.3s,
        transform 0.3s;
}
.notify-toast-leave-active {
    transition:
        opacity 0.25s,
        transform 0.25s;
}
.notify-toast-enter-from {
    opacity: 0;
    transform: translateX(40px);
}
.notify-toast-leave-to {
    opacity: 0;
    transform: translateX(40px);
}
.notify-toast-leave-active {
    position: absolute;
}
</style>

<style scoped>
/* ════════════════════════════════════════════
   桌位狀態封鎖畫面
   ════════════════════════════════════════════ */
.gate-wrap {
    min-height: 100vh;
    background: #1e100b;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 1rem;
    padding: 2rem;
    text-align: center;
}
.gate-icon {
    font-size: 3.5rem;
    opacity: 0.55;
    margin-bottom: 0.5rem;
}
.gate-title {
    font-size: 1.6rem;
    font-style: italic;
    color: #e3c76b;
    margin: 0;
}
.gate-body {
    font-size: 1rem;
    font-style: italic;
    color: rgba(249, 221, 211, 0.6);
    margin: 0;
    max-width: 26rem;
    line-height: 1.7;
}
.gate-sub {
    font-size: 0.75rem;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: rgba(208, 197, 181, 0.35);
    margin-top: 0.5rem;
}
/* loading spinner */
.gate-spinner {
    width: 36px;
    height: 36px;
    border: 3px solid rgba(227, 199, 107, 0.15);
    border-top-color: #e3c76b;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
    margin-bottom: 0.5rem;
}
@keyframes spin {
    to {
        transform: rotate(360deg);
    }
}
.gate-hint {
    font-size: 0.8rem;
    letter-spacing: 0.18em;
    color: rgba(208, 197, 181, 0.4);
    margin: 0;
}
@import url('https://fonts.googleapis.com/css2?family=Noto+Serif+TC:wght@400;700&family=Newsreader:ital,wght@0,400;0,600;1,400&family=Work+Sans:wght@300;400&family=Cormorant+Garamond:ital,wght@1,400;1,600&display=swap');

.root-wrap {
    background: #1e100b;
    color: #f9ddd3;
    font-family: 'Newsreader', serif;
    /* 鎖死整頁：flex 直欄，不讓 body 出捲軸 */
    height: 100vh;
    height: 100dvh;
    overflow: hidden;
    display: flex;
    flex-direction: column;
}

/* ════════════════════════════════════════════
   全寬頂部 Header
   ════════════════════════════════════════════ */
.dine-header {
    flex-shrink: 0;
    z-index: 60;
    height: 4rem;
    background: #180b06;
    border-bottom: 1px solid rgba(77, 70, 58, 0.4);
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.35);
    display: flex;
    align-items: center;
    padding: 0 1rem;
    gap: 1rem;
}

/* 左：Logo + 今日菜單 */
.dh-logo {
    flex-shrink: 0;
    display: flex;
    align-items: center;
    gap: 0.6rem;
}
.dh-logo-img {
    height: 3.2rem;
    width: auto;
    object-fit: contain;
}
.dh-title {
    font-size: 1.5rem;
    color: #e3c76b;
    font-style: italic;
    margin: 0;
    line-height: 1;
    letter-spacing: 0.08em;
    white-space: nowrap;
}

/* 中：彈性填充 */
.dh-brand {
    flex: 1;
}

/* 右：桌號 ｜ 人數 ｜ 會員，同一行 */
.dh-right {
    flex-shrink: 0;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}
.dh-label {
    font-size: 1rem;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: rgba(208, 197, 181, 0.5);
    white-space: nowrap;
}
.dh-table-name {
    color: #e3c76b;
    font-size: 0.95rem;
    font-style: italic;
}

/* 分隔線 */
.dh-sep {
    width: 1px;
    height: 1.5rem;
    background: rgba(77, 70, 58, 0.5);
    flex-shrink: 0;
    margin: 0 0.4rem;
}

/* 人數按鈕 */
.dh-pax-btn {
    width: 26px;
    height: 26px;
    border: 1px solid rgba(77, 70, 58, 0.7);
    background: #2b1c16;
    color: #f9ddd3;
    border-radius: 0.2rem;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 1.1rem;
    line-height: 1;
    transition:
        border-color 0.3s,
        color 0.3s;
}
.dh-pax-btn:hover {
    border-color: #e3c76b;
    color: #e3c76b;
}

/* 人數輸入框 */
.dh-pax-input {
    width: 2.2rem;
    text-align: center;
    background: transparent;
    border: none;
    border-bottom: 1px solid rgba(227, 199, 107, 0.4);
    color: #f9ddd3;
    font-size: 0.95rem;
    outline: none;
    padding: 0;
    -moz-appearance: textfield;
    transition: border-color 0.3s;
}
.dh-pax-input::-webkit-outer-spin-button,
.dh-pax-input::-webkit-inner-spin-button {
    -webkit-appearance: none;
}
.dh-pax-input:focus {
    border-bottom-color: #e3c76b;
}
.dh-pax-input::placeholder {
    color: rgba(208, 197, 181, 0.4);
}

/* 人數未填警示（閒置閃爍） */
.pax-empty {
    border-bottom-color: rgba(227, 199, 107, 0.7);
    animation: pax-pulse 1.6s ease-in-out infinite;
}
@keyframes pax-pulse {
    0%,
    100% {
        border-bottom-color: rgba(227, 199, 107, 0.35);
    }
    50% {
        border-bottom-color: rgba(227, 199, 107, 0.9);
    }
}

/* 送出驗證失敗：搖晃 + 橘色 */
@keyframes pax-shake {
    0%,
    100% {
        transform: translateX(0);
    }
    20% {
        transform: translateX(-6px);
    }
    40% {
        transform: translateX(6px);
    }
    60% {
        transform: translateX(-4px);
    }
    80% {
        transform: translateX(4px);
    }
}
.pax-label-error {
    color: #ffb347 !important;
    animation: pax-shake 0.45s ease;
}
.pax-input-error {
    border-bottom-color: #ffb347 !important;
    color: #ffb347 !important;
    animation: pax-shake 0.45s ease;
}
.pax-ctrl-error {
    border-color: #ffb347 !important;
    color: #ffb347 !important;
    animation: pax-shake 0.45s ease;
}

/* 右：會員 */
.dh-member {
    flex-shrink: 0;
    display: flex;
    align-items: center;
    gap: 0.4rem;
    min-width: 9rem;
    white-space: nowrap;
    position: relative;
}
.dh-member-btn {
    cursor: pointer;
    padding: 0.25rem 0.5rem;
    border-radius: 0.375rem;
    transition: background 0.2s;
}
.dh-member-btn:hover {
    background: rgba(227, 199, 107, 0.07);
}
.dh-member-label {
    font-size: 0.9rem;
    letter-spacing: 0.08em;
    color: rgba(208, 197, 181, 0.75);
}
.dh-chevron {
    color: rgba(208, 197, 181, 0.4);
    transition: transform 0.2s;
}
.dh-chevron.open {
    transform: rotate(180deg);
}

/* 下拉選單 */
.member-dropdown {
    position: absolute;
    top: calc(100% + 0.5rem);
    right: 0;
    min-width: 9rem;
    background: #2b1c16;
    border: 1px solid rgba(77, 70, 58, 0.45);
    border-radius: 0.5rem;
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
    overflow: hidden;
    z-index: 100;
}
.member-dropdown-item {
    width: 90%;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.65rem 1rem;
    font-size: 0.8rem;
    letter-spacing: 0.1em;
    color: rgba(208, 197, 181, 0.7);
    background: none;
    border: none;
    cursor: pointer;
    transition:
        background 0.15s,
        color 0.15s;
    text-align: left;
}
.member-dropdown-item:hover {
    background: rgba(227, 199, 107, 0.07);
    color: #e3c76b;
}
.member-backdrop {
    position: fixed;
    inset: 0;
    z-index: 99;
}

/* ═══ 手機分類捲動列 ═══ */
.mobile-header {
    display: none;
    position: sticky;
    top: var(--dine-header-h);
    z-index: 50;
    background: #180b06;
    margin-bottom: -0.8rem;
}
.mobile-cat-tabs {
    display: flex;
    overflow-x: auto;
    padding: 0.4rem 0.75rem 0.55rem;
    scrollbar-width: none;
    gap: 0;
}
.mobile-cat-tabs::-webkit-scrollbar {
    display: none;
}
.mobile-cat-btn {
    white-space: nowrap;
    padding: 0.3rem 0.85rem;
    font-family: 'Work Sans', sans-serif;
    font-size: 0.9rem;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: rgba(208, 197, 181, 0.5);
    border: none;
    border-bottom: 2px solid transparent;
    background: transparent;
    cursor: pointer;
    transition: all 0.25s;
    display: flex;
    align-items: center;
    gap: 0.3rem;
}
.mobile-cat-btn.active {
    color: #e3c76b;
    border-bottom-color: #e3c76b;
}
.cat-badge {
    font-size: 0.55rem;
    background: rgba(77, 70, 58, 0.5);
    color: rgba(208, 197, 181, 0.5);
    border-radius: 99px;
    padding: 0.05rem 0.35rem;
}

/* ═══ 三欄佈局 ═══ */
.page-layout {
    display: grid;
    grid-template-columns: 210px 1fr 360px;
    flex: 1; /* 填滿 root-wrap 剩餘高度 */
    min-height: 0; /* flex 子項必須，否則 grid 不縮小 */
    overflow: hidden; /* 整個版面不捲動 */
}

/* ═══ Sidebar ═══ */
.sidebar-left {
    background: #180b06;
    border-right: 1px solid rgba(77, 70, 58, 0.2);
}
/* ----網頁版---- */
@media (min-width: 1101px) {
    .dine-header-mobile {
        display: none;
    } /* 網頁版隱藏手機板 header */
    .sidebar-left {
        height: 100%; /* 撐滿 grid 格 */
        overflow-y: auto;
    }
    /* 訂單面板：Teleport 到 body 後以 fixed 固定在右側欄 */
    .order-panel {
        position: fixed;
        top: 4rem; /* 與 dine-header 同高 */
        right: 0;
        width: 360px; /* 對齊 page-layout 第三欄寬度 */
        height: calc(100vh - 4rem);
    }
}
.cat-link {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    padding: 0.7rem 1.5rem;
    font-family: 'Work Sans', sans-serif;
    font-size: 1.2rem;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: rgba(208, 197, 181, 0.6);
    border-left: 2px solid transparent;
    border-right: none;
    border-top: none;
    border-bottom: none;
    background: transparent;
    cursor: pointer;
    transition: all 0.3s;
    width: 100%;
}
.cat-link:hover {
    color: #e3c76b;
    background: rgba(227, 199, 107, 0.04);
}
.cat-link.active {
    color: #e3c76b;
    border-left-color: #e3c76b;
    background: rgba(227, 199, 107, 0.06);
}
.cat-count {
    margin-left: auto;
    font-size: 1rem;
    color: rgba(208, 197, 181, 0.35);
}
/* 今日推薦 / 主廚特選 特殊分類的分隔線 */
.cat-link.cat-special {
    color: rgba(227, 199, 107, 0.75);
    font-size: 1.2rem;
}
.cat-link.cat-special.active {
    color: #e3c76b;
}
.cat-link.cat-special .cat-count {
    color: rgba(227, 199, 107, 0.4);
}
.cat-divider {
    height: 1px;
    background: linear-gradient(90deg, rgba(77, 70, 58, 0.5), transparent);
    margin: 0.35rem 1.5rem;
}

/* ═══ 歷史訂單 ═══ */
.history-card {
    background: rgba(43, 28, 22, 0.6);
    border: 1px solid rgba(77, 70, 58, 0.4);
    border-radius: 0.5rem;
    padding: 1rem 1.25rem;
    margin-bottom: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}
.history-meta {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
}
.history-items {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
}
.history-item-wrap {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
}
.history-setmeal-badge {
    display: inline-block;
    font-size: 0.58rem;
    letter-spacing: 0.15em;
    text-transform: uppercase;
    padding: 0.1rem 0.4rem;
    border-radius: 0.2rem;
    background: rgba(93, 69, 20, 0.4);
    border: 1px solid rgba(228, 194, 133, 0.35);
    color: #e4c285;
    margin-right: 0.35rem;
    vertical-align: middle;
}
.history-subitems {
    display: flex;
    flex-direction: column;
    gap: 0.1rem;
    padding-left: 1rem;
    border-left: 2px solid rgba(77, 70, 58, 0.4);
    margin-left: 0.25rem;
}
.history-subitem {
    font-size: 0.8rem;
    color: rgba(208, 197, 181, 0.6);
}
.history-reorder-btn {
    align-self: flex-end;
    padding: 0.5rem 1.25rem;
    background: linear-gradient(to right, #e3c76b, #c6ab53);
    color: #3b2f00;
    border: none;
    border-radius: 0.375rem;
    font-size: 0.8rem;
    letter-spacing: 0.15em;
    cursor: pointer;
    transition: filter 0.2s;
}
.history-reorder-btn:hover {
    filter: brightness(1.08);
}

/* ═══ Menu main ═══ */
.menu-main {
    background: #1e100b;
}
/* ----網頁版---- */
@media (min-width: 1101px) {
    .menu-main {
        height: 100%; /* 撐滿 grid 格，只有這欄捲動 */
        overflow-y: auto;
    }
}
.menu-sections {
    padding: 0 1.25rem;
}
/* ----網頁版---- */
@media (min-width: 1101px) {
    .menu-sections {
        padding: 0 1.5rem;
    }
}

/* ═══ Toolbar ═══ */
.toolbar {
    position: sticky;
    top: 0;
    z-index: 20;
    background: #1e100b;
    border-bottom: 1px solid rgba(77, 70, 58, 0.25);
    padding: 1rem 1rem;
}
.mobile-search-wrap {
    display: none;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
}
.toolbar-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: nowrap;
}
.chips-wrap {
    display: flex;
    gap: 0.4rem;
    overflow-x: auto;
    flex: 1 1 0;
    min-width: 0;
    scrollbar-width: none;
}
.chips-wrap::-webkit-scrollbar {
    display: none;
}
.view-toggle {
    display: flex;
    flex-shrink: 0;
}

/* ═══ Order Panel ═══ */
.order-panel {
    pointer-events: auto !important;
    z-index: 50 !important;
    background: #180b06;
    border-left: 1px solid rgba(77, 70, 58, 0.2);
    display: flex;
    flex-direction: column;
    overflow: hidden;
    /* height 由各平台 @media 定義：
       網頁版 → calc(100vh - 4rem)（desktop @media）
       手機板 → inset: 0 隱含 100vh（mobile @media）   */
}
.panel-header {
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.75rem 1rem;
    border-bottom: 1px solid rgba(77, 70, 58, 0.2);
}
/* 餐點清單：可捲動，撐滿剩餘空間 */
.panel-items {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
    padding: 0.25rem 1rem;
}
/* 底部：備註 + 合計 + 按鈕，固定不捲 */
.panel-footer {
    flex-shrink: 0;
    border-top: 1px solid rgba(77, 70, 58, 0.2);
}
.panel-close-btn {
    display: none;
    background: none;
    border: none;
    cursor: pointer;
    color: rgba(208, 197, 181, 0.7);
    font-size: 1.5rem;
    line-height: 1;
    padding: 0;
}

/* ═══ 手機底部列 ═══ */
.mobile-bottom-bar {
    display: none;
}
.mobile-overlay {
    display: none;
}

/* ════════════════════════════════════════════════════════════════
   ----手機板----   ≤ 1100px  版面結構覆寫
   ════════════════════════════════════════════════════════════════ */
@media (max-width: 1100px) {
    /* ── App-shell：body 鎖死，讓 menu-main 內部捲動 ──
       order-panel / overlay 已 Teleport 到 body，不受 overflow:hidden 影響 */
    html:has(.root-wrap),
    body:has(.root-wrap) {
        overflow: hidden !important;
        height: 100% !important;
    }
    .root-wrap {
        height: 100vh;
        height: 100dvh; /* 排除手機 URL bar */
        overflow: hidden;
        display: flex;
        flex-direction: column;
    }

    /* ----手機板---- 切換 header */
    .dine-header {
        display: none;
    } /* 隱藏網頁版 header */
    .dine-header-mobile {
        display: grid;
    } /* 顯示手機板 header（grid 由 .dine-header-mobile 自身定義） */

    .toolbar {
        top: 0;
        z-index: 10;
        background: #180b06;
    }

    .mobile-header {
        display: block;
        flex-shrink: 0; /* 不讓 header 被壓縮 */
    }
    .mobile-search-wrap {
        display: flex;
    }
    .page-layout {
        grid-template-columns: 1fr;
        flex: 1; /* 撐滿 root-wrap 剩餘高度 */
        min-height: 0; /* flex 子項必須，否則不縮小 */
        overflow: hidden;
    }
    .sidebar-left {
        display: none;
    }
    /* menu-main 成為唯一捲動容器 */
    .menu-main {
        height: 100%;
        overflow-y: auto;
        -webkit-overflow-scrolling: touch; /* iOS 慣性捲動 */
        padding-bottom: 3rem; /* 避免底部列遮住最後一道菜 */
    }

    /* Order panel → full-height bottom sheet */
    .order-panel {
        position: fixed;
        top: 4.2rem;
        left: 0;
        right: 0;
        bottom: 0;
        z-index: 200 !important;
        transform: translateY(100%);
        transition: transform 0.35s cubic-bezier(0.4, 0, 0.2, 1);
        border-left: none;
        border-top: 1px solid rgba(77, 70, 58, 0.4);
        pointer-events: auto;
    }
    .order-panel.panel-open {
        transform: translateY(0);
    }
    .order-panel.panel-open * {
        pointer-events: auto;
    }
    .panel-close-btn {
        display: block;
    }

    .mobile-bottom-bar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        z-index: 80;
        background: linear-gradient(135deg, #2b1c16 0%, #1e100b 100%);
        border-top: 1px solid rgba(77, 70, 58, 0.4);
        padding: 0.85rem 1.25rem;
        cursor: pointer;
        box-shadow: 0 -4px 20px rgba(0, 0, 0, 0.4);
    }
    /* Teleport 到 body，z-index 低於 order-panel(200)，高於 bottom-bar(80) */
    .mobile-overlay {
        display: block;
        position: fixed;
        top: 3.8rem;
        left: 0;
        right: 0;
        bottom: 0;
        z-index: 150;
        background: rgba(24, 11, 6, 0.78);
        pointer-events: auto;
    }
}

.bottom-badge {
    min-width: 4.5rem;
    height: 1.5rem;
    border-radius: 99px;
    background: linear-gradient(to right, #e3c76b, #c6ab53);
    color: #3b2f00;
    font-family: 'Work Sans', sans-serif;
    font-size: 1rem;
    font-weight: 450;
    display: flex;
    align-items: center;
    justify-content: center;
}
.bottom-cta {
    background: linear-gradient(to right, #e3c76b, #c6ab53);
    color: #3b2f00;
    padding: 0.45rem 1rem;
    border-radius: 0.125rem;
}

/* ════════════════════════════════════════════════════════════════
   ----網頁版----   ≥ 1101px  餐點卡片
   ════════════════════════════════════════════════════════════════ */
@media (min-width: 1101px) {
    /* ═══ Dish rows ═══ */
    .dish-row {
        display: grid;
        grid-template-columns: 160px 1fr; /* 列表視圖：圖片 + 內容（按鈕已併入內容欄） */
        align-items: stretch;
        background: #362620;
        border-radius: 0.75rem;
        cursor: pointer;
        transition:
            transform 0.45s cubic-bezier(0.4, 0, 0.2, 1),
            box-shadow 0.45s ease;
    }
    .dish-row:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(0, 0, 0, 0.35);
    }
    .dish-row.dish-row-grid {
        grid-template-columns: 1fr;
        display: flex;
        flex-direction: column;
        overflow: hidden; /* 讓圓角裁切圖片頂部 */
    }
    .dish-img {
        width: 160px;
        height: 120px;
        object-fit: cover;
        align-self: center;
        display: block;
        flex-shrink: 0;
        transition: transform 0.7s cubic-bezier(0.4, 0, 0.2, 1);
    }
    .dish-row:hover .dish-img {
        transform: scale(1.05);
    }
    .dish-row-grid .dish-img {
        width: 100%;
        height: 140px;
    }
    .dish-img-placeholder {
        width: 160px;
        height: 120px;
        align-self: center;
        background: #2b1c16;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        color: rgba(208, 197, 181, 0.15);
        font-size: 1.8rem;
    }
    .dish-row-grid .dish-img-placeholder {
        width: 100%;
        height: 140px;
    }
    /* 列表視圖：內容欄垂直置中，確保上下間距一致 */
    .dish-content {
        display: flex;
        flex-direction: column;
        justify-content: center;
        padding: 0.75rem 0.75rem;
    }
    /* 網格卡片：內容區填滿，靠上對齊 */
    .dish-content-grid {
        flex: 1;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: flex-start; /* 靠上，不留空白 */
        text-align: center;
    }
    /* Badge 列 */
    .dish-badges {
        display: flex;
        flex-wrap: wrap;
        gap: 0.3rem;
        margin: 0.4rem 0;
    }
    /* 網格視圖：badge 置中 */
    .dish-content-grid .dish-badges {
        justify-content: center;
    }
    /* 網格底部：價格 + 按鈕，固定在卡片底部，置中 */
    .grid-footer {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 0 0.75rem;
        border-top: 1px solid rgba(77, 70, 58, 0.25);
    }
    .grid-price {
        color: #d5b478;
        font-size: 1rem;
        letter-spacing: 0.08em;
        text-align: center;
    }
    /* 列表視圖底列：價格左、按鈕右，同一行 */
    .list-footer {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-top: auto;
        padding-top: 0.4rem;
    }
    .list-footer p {
        margin: 0; /* 移除 <p> 預設 margin，讓價格與按鈕垂直對齊 */
        line-height: 1;
    }
    .qty-col {
        display: flex;
        flex-direction: row;
        align-items: center;
        gap: 0.4rem;
    }
    .qty-row {
        display: flex;
        flex-direction: row;
        align-items: center;
        gap: 0.4rem;
        margin-bottom: 0.8rem;
        margin-top: auto;
    }
    .qty-num {
        color: #f9ddd3;
        width: 40px;
        text-align: center;
    }
    .qty-num.active {
        color: #e3c76b;
    }
    .dishes-wrap.grid-view {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
        gap: 0.75rem;
    }
} /* end ----網頁版---- 餐點卡片 */

/* ═══ Misc ═══ */
.candle-glow {
    background: radial-gradient(
        ellipse 80% 60% at 50% 0%,
        rgba(227, 199, 107, 0.1) 0%,
        transparent 70%
    );
}
.feather-divider {
    height: 1px;
    background: linear-gradient(90deg, transparent, #e4c285 50%, transparent);
    position: relative;
}
.feather-divider::after {
    content: '◈';
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%);
    color: #e4c285;
    font-size: 0.7rem;
    background: #1e100b;
    padding: 0 0.65rem;
}
/* ----網頁版---- 餐點名稱 & 區塊標題 */
@media (min-width: 1101px) {
    .dish-name {
        font-size: 1.4rem;
    }
    .dish-row-grid .dish-name {
        font-size: 1.285rem;
        text-align: center;
        width: 100%;
        display: -webkit-box;
        -webkit-box-orient: vertical;
        overflow: hidden;
    }
    .section-title {
        font-family: 'Noto Serif TC', serif;
        font-style: italic;
        font-size: 2rem;
        color: #e3c76b;
        padding-top: 0;
        margin-bottom: 0.85rem;
    }
} /* end ----網頁版---- 餐點名稱 & 區塊標題 */
.status-msg {
    text-align: center;
    padding: 5rem 1rem;
    font-family: 'Newsreader', serif;
    font-style: italic;
    color: rgba(249, 221, 211, 0.4);
}
.order-item {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid rgba(77, 70, 58, 0.25);
}
.order-item:last-child {
    border-bottom: none;
}
/* 套餐列 */
.setmeal-order-item {
    background: rgba(227, 199, 107, 0.03);
    border-left: 2px solid rgba(227, 199, 107, 0.25);
}
.setmeal-badge {
    font-size: 0.65rem;
    letter-spacing: 0.1em;
    color: rgba(227, 199, 107, 0.7);
    margin-bottom: 0.2rem;
}
.setmeal-subitems {
    display: flex;
    flex-direction: column;
    gap: 0;
    margin-top: 0.25rem;
    padding: 0 0.2rem 0.1rem;
}
.setmeal-subitem-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.08rem 0;
}
.setmeal-subitem-name {
    font-size: 0.72rem;
    color: rgba(208, 197, 181, 0.55);
    letter-spacing: 0.03em;
    flex: 1;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.setmeal-subitem-qty {
    font-size: 0.72rem;
    color: rgba(208, 197, 181, 0.4);
    letter-spacing: 0.04em;
    flex-shrink: 0;
    margin-left: 0.5rem;
}

/* 贈品列 */
.gift-order-item {
    background: rgba(163, 217, 119, 0.04);
}
.gift-order-badge {
    font-size: 0.68rem;
    letter-spacing: 0.1em;
    color: #a3d977;
    background: rgba(163, 217, 119, 0.12);
    border: 1px solid rgba(163, 217, 119, 0.3);
    border-radius: 0.2rem;
    padding: 0.05rem 0.35rem;
    display: inline-block;
}
.order-item-top {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}
.order-item-note {
    font-size: 1rem;
    color: rgba(208, 197, 181, 0.45);
    margin: 0;
    padding-left: 0.1rem;
    white-space: pre-wrap;
    word-break: break-all;
}
.order-item-name-wrap {
    flex: 1;
    min-width: 0;
    cursor: pointer;
    border-radius: 0.25rem;
    padding: 0.1rem 0.1rem;
    margin: -0.1rem -0.2rem;
    transition: background 0.15s;
}
.order-item-name-wrap:hover {
    background: rgba(227, 199, 107, 0.07);
}
.order-item-name {
    margin: 0.1rem 0;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-size: 1rem;
}
.order-item-right {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    flex-shrink: 0;
}
.order-item-price {
    padding-right: 0.6rem;
    font-size: 1rem;
    letter-spacing: 0.08em;
    color: #d5b478;
    white-space: nowrap;
}
.order-item-qty {
    font-size: 1rem;
    width: 1.5rem;
    text-align: center;
}

/* ═══ Buttons ═══ */
@media (min-width: 1101px) {
    .qty-btn {
        pointer-events: auto !important;
        cursor: pointer !important;
        width: 30px;
        height: 30px;
        border: 1px solid rgba(77, 70, 58, 0.7);
        background: #2b1c16;
        color: #f9ddd3;
        border-radius: 0.125rem;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        font-size: 1.5rem;
        line-height: 1;
        transition:
            border-color 0.3s,
            color 0.3s;
    }
    .qty-btn:hover {
        border-color: #e3c76b;
        color: #e3c76b;
    }
} /* end ----網頁版---- qty-btn */
/* 購物車專用 +/- 按鈕（較小） */
.qty-btn-order {
    width: 24px;
    height: 24px;
    border: 1px solid rgba(77, 70, 58, 0.6);
    background: #2b1c16;
    color: #f9ddd3;
    border-radius: 0.125rem;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 1.1rem;
    line-height: 1;
    transition:
        border-color 0.3s,
        color 0.3s;
    flex-shrink: 0;
}
.qty-btn-order:hover {
    border-color: #e3c76b;
    color: #e3c76b;
}
.chip-label {
    font-family: 'Work Sans', sans-serif;
    font-size: 0.7rem;
    letter-spacing: 0.15em;
    text-transform: uppercase;
    color: rgba(208, 197, 181, 0.45);
    white-space: nowrap;
    flex-shrink: 0;
    align-self: center;
}
.chip-btn {
    font-family: 'Work Sans', sans-serif;
    font-size: 1rem;
    letter-spacing: 0.15em;
    text-transform: uppercase;
    padding: 0.28rem 0.8rem;
    border-radius: 0.75rem;
    background: #5d4514;
    color: #e4c285;
    border: 1px solid transparent;
    cursor: pointer;
    transition: all 0.3s;
    user-select: none;
    white-space: nowrap;
}
.chip-btn:hover {
    border-color: rgba(228, 194, 133, 0.5);
}
.chip-btn.active {
    border-color: #e4c285;
    box-shadow:
        0 0 16px rgba(228, 194, 133, 0.35),
        inset 0 0 8px rgba(228, 194, 133, 0.07);
    color: #fee182;
}
.view-btn {
    padding: 0.35rem 0.65rem;
    font-family: 'Work Sans', sans-serif;
    font-size: 0.7rem;
    color: rgba(208, 197, 181, 0.5);
    border: 1px solid rgba(77, 70, 58, 0.5);
    cursor: pointer;
    transition: all 0.3s;
    background: transparent;
}
.view-btn.active {
    color: #e3c76b;
    border-color: #e3c76b;
    background: rgba(227, 199, 107, 0.06);
}
.submit-btn {
    background: linear-gradient(to right, #e3c76b, #c6ab53);
    color: #3b2f00;
    display: block;
    font-family: 'Work Sans', sans-serif;
    cursor: pointer;
    border: none;
}
.clear-btn {
    border: 1px solid rgba(77, 70, 58, 0.5);
    color: rgba(208, 197, 181, 0.7);
    background: none;
    cursor: pointer;
    font-family: 'Work Sans', sans-serif;
}
.clear-btn:hover {
    border-color: rgba(255, 100, 80, 0.5);
    color: #ffb4ab;
}

/* ═══ 備註文字框 ═══ */
.note-textarea {
    width: 100%;
    background: rgba(43, 28, 22, 0.6);
    border: 1px solid rgba(227, 199, 107, 0.35);
    border-radius: 0.25rem;
    color: #f9ddd3;
    font-size: 0.85rem;
    padding: 0.6rem 0.75rem;
    outline: none;
    transition:
        border-color 0.3s,
        box-shadow 0.3s;
    line-height: 1.5;
}
.note-textarea::placeholder {
    color: rgba(208, 197, 181, 0.3);
}
.note-textarea:focus {
    border-color: rgba(227, 199, 107, 0.7);
    box-shadow: 0 0 0 2px rgba(227, 199, 107, 0.08);
}

/* ═══ Form ═══ */
.input-line {
    background: transparent;
    border: none;
    border-bottom: 1px solid rgba(77, 70, 58, 0.65);
    border-radius: 0;
    color: #f9ddd3;
    font-family: 'Newsreader', serif;
    font-size: 0.95rem;
    padding: 0.5rem 0;
    outline: none;
    width: 100%;
    transition:
        border-color 0.4s,
        box-shadow 0.4s;
}
.input-line:focus {
    border-bottom-color: #e3c76b;
    box-shadow: 0 4px 12px -4px rgba(227, 199, 107, 0.22);
}
.input-line::placeholder {
    color: rgba(208, 197, 181, 0.3);
}

/* 優惠券下拉選單 */
.coupon-select {
    width: 100%;
    background: rgba(24, 11, 6, 0.6);
    border: 1px solid rgba(77, 70, 58, 0.6);
    border-radius: 0.25rem;
    color: #f9ddd3;
    font-family: 'Newsreader', serif;
    font-size: 0.85rem;
    padding: 0.45rem 0.65rem;
    outline: none;
    cursor: pointer;
    transition: border-color 0.25s;
    appearance: none;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='10' height='6' fill='none'%3E%3Cpath d='M1 1l4 4 4-4' stroke='%23e3c76b' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
    background-repeat: no-repeat;
    background-position: right 0.65rem center;
    padding-right: 2rem;
}
.coupon-select:focus {
    border-color: rgba(227, 199, 107, 0.6);
}
.coupon-select option {
    background: #2b1c16;
    color: #f9ddd3;
}
/* 訪客優惠券區：登入會員按鈕 */
.coupon-login-btn {
    width: 100%;
    padding: 0.5rem 0.75rem;
    background: transparent;
    border: 1px solid rgba(227, 199, 107, 0.45);
    border-radius: 0.25rem;
    color: #e3c76b;
    font-size: 0.75rem;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    cursor: pointer;
    transition:
        background 0.2s,
        border-color 0.2s;
    text-align: center;
}
.coupon-login-btn:hover {
    background: rgba(227, 199, 107, 0.08);
    border-color: rgba(227, 199, 107, 0.7);
}

/* 下拉選單最底部「查看更多優惠券」特殊項目 */
.coupon-goto-option {
    color: #e3c76b;
    border-top: 1px solid rgba(77, 70, 58, 0.4);
}
.table-id-input {
    background: transparent;
    border: none;
    border-bottom: 1px solid rgba(227, 199, 107, 0.35);
    color: #e3c76b;
    width: 8rem;
    outline: none;
    padding-left: 5px;
    transition: border-color 0.3s;
    -moz-appearance: textfield;
    font-family: 'Noto Serif TC', serif;
    font-style: italic;
}
.table-id-input::-webkit-outer-spin-button,
.table-id-input::-webkit-inner-spin-button {
    -webkit-appearance: none;
}
.table-id-input:focus {
    border-bottom-color: #e3c76b;
}
.table-id-input::placeholder {
    color: rgba(227, 199, 107, 0.3);
    font-size: 0.85rem;
}
.table-id-select {
    pointer-events: auto !important;
    cursor: pointer !important;
    background: #2b1c16;
    border: none;
    border-bottom: 1px solid rgba(227, 199, 107, 0.45);
    color: #e3c76b;
    font-family: 'Noto Serif TC', serif;
    font-style: italic;
    font-size: 1rem;
    padding: 0.1rem 0.25rem;
    outline: none;
    cursor: pointer;
    min-width: 6rem;
    transition: border-color 0.3s;
}
.table-id-select:focus {
    border-bottom-color: #e3c76b;
}
.table-id-select option {
    background: #2b1c16;
    color: #f9ddd3;
    font-style: normal;
}

/* ═══ Badges ═══ */
.badge-new {
    background: rgba(227, 199, 107, 0.18);
    border: 1px solid rgba(227, 199, 107, 0.45);
    color: #e3c76b;
}
.badge-veg {
    background: rgba(66, 49, 43, 0.5);
    border: 1px solid rgba(120, 180, 80, 0.4);
    color: #a3d977;
}
.badge-spicy {
    background: rgba(147, 0, 10, 0.25);
    border: 1px solid rgba(255, 100, 80, 0.4);
    color: #ffb4ab;
}
.badge-chef {
    background: rgba(93, 69, 20, 0.4);
    border: 1px solid rgba(228, 194, 133, 0.35);
    color: #e4c285;
}
.badge {
    font-family: 'Work Sans', sans-serif;
    font-size: 0.55rem;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    padding: 0.15rem 0.45rem;
}

/* ═══ 通知型活動 ═══ */
.notify-events {
    display: flex;
    flex-direction: column;
    gap: 0.45rem;
    margin-top: 0.5rem;
}
.notify-event-card {
    border-radius: 0.4rem;
    padding: 0.55rem 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
}
.notify-event-card.eligible {
    background: rgba(227, 199, 107, 0.08);
    border: 1px solid rgba(227, 199, 107, 0.35);
}
.notify-event-card.not-eligible {
    background: rgba(77, 70, 58, 0.2);
    border: 1px solid rgba(77, 70, 58, 0.35);
}
.notify-event-top {
    display: flex;
    align-items: center;
    gap: 0.4rem;
}
.notify-event-icon {
    font-size: 0.85rem;
    flex-shrink: 0;
}
.notify-event-title {
    font-size: 0.78rem;
    color: #e3c76b;
    letter-spacing: 0.06em;
    flex: 1;
}
.notify-event-badge {
    font-size: 0.65rem;
    letter-spacing: 0.06em;
    padding: 0.1rem 0.45rem;
    border-radius: 99px;
}
.eligible .notify-event-badge {
    background: rgba(163, 217, 119, 0.15);
    color: #a3d977;
    border: 1px solid rgba(163, 217, 119, 0.4);
}
.not-eligible .notify-event-badge {
    background: rgba(208, 197, 181, 0.08);
    color: rgba(208, 197, 181, 0.5);
    border: 1px solid rgba(208, 197, 181, 0.2);
}
.notify-event-desc {
    font-size: 0.72rem;
    color: rgba(208, 197, 181, 0.55);
    margin: 0;
    padding-left: 1.3rem;
}

/* 訪客登入即享提示卡 */
.notify-event-card.guest-login-hint {
    background: rgba(227, 199, 107, 0.06);
    border: 1px solid rgba(227, 199, 107, 0.4);
    border-left: 3px solid #e3c76b;
}
.notify-event-card.guest-login-hint .notify-event-desc {
    color: rgba(227, 199, 107, 0.6);
}
.eligible-badge {
    background: rgba(163, 217, 119, 0.15);
    color: #a3d977;
    border: 1px solid rgba(163, 217, 119, 0.4);
}
/* 快到門檻提示卡 */
.notify-event-card.near-threshold {
    background: rgba(255, 160, 80, 0.07);
    border: 1px solid rgba(255, 160, 80, 0.35);
    border-left: 3px solid #ff9f4a;
}
.notify-event-card.near-threshold .notify-event-title {
    color: #ffb870;
}
.notify-event-card.near-threshold .notify-event-desc {
    color: rgba(255, 184, 112, 0.7);
}
.near-badge {
    background: rgba(255, 160, 80, 0.15);
    color: #ff9f4a;
    border: 1px solid rgba(255, 160, 80, 0.4);
}

/* 已套用活動標籤 */
.applied-event-tag {
    display: flex;
    align-items: flex-start;
    gap: 0.4rem;
    background: rgba(227, 199, 107, 0.07);
    border: 1px solid rgba(227, 199, 107, 0.3);
    border-left: 3px solid #e3c76b;
    border-radius: 0.4rem;
    padding: 0.45rem 0.7rem;
}
.applied-event-icon {
    font-size: 0.85rem;
    flex-shrink: 0;
    margin-top: 0.05rem;
}
.applied-event-text {
    font-size: 0.72rem;
    color: rgba(227, 199, 107, 0.85);
    letter-spacing: 0.04em;
    line-height: 1.5;
}

/* ═══ Toast ═══ */
.toast-wrap {
    position: fixed;
    bottom: 5rem;
    left: 50%;
    z-index: 300;
    transition: all 0.35s;
}
@media (min-width: 1101px) {
    .toast-wrap {
        bottom: 2rem;
    }
}

/* ═══ Scrollbar ═══ */
::-webkit-scrollbar {
    width: 4px;
    height: 4px;
}
::-webkit-scrollbar-track {
    background: transparent;
}
::-webkit-scrollbar-thumb {
    background: #4d463a;
    border-radius: 3px;
}

/* ═══ Font helpers ═══ */
.font-body {
    font-family: 'Newsreader', serif;
}
.font-headline {
    font-family: 'Noto Serif TC', serif;
}
.font-label {
    font-family: 'Work Sans', sans-serif;
}

/* 覆蓋 bootstrap-icons 造成的樣式污染 */
.btn-success,
.btn-danger,
.btn-info {
    display: none !important;
}

/* ════════════════════════════════════════════════════════════════
   ----手機板----   dine-header-mobile（雙列 header）
   只在手機板顯示，網頁版 .dine-header 仍保留原樣
   ════════════════════════════════════════════════════════════════ */
.dine-header-mobile {
    /* display 不在此設定，由下方 media query 各自控制：
       手機板 @media (max-width: 1100px) → display: grid
       網頁版 @media (min-width: 1101px) → display: none          */

    /* Grid 結構定義（display 由 media query 控制） */
    grid-template-columns: auto 1fr;
    grid-template-rows: 1fr 1fr; /* 兩行等高，讓右側各佔一半 */
    column-gap: 0.65rem;
    row-gap: 0;
    align-items: center; /* 各格垂直置中 */

    background: #180b06;
    padding: 0.4rem 0.85rem;
    position: relative;
    z-index: 60;
    min-height: 3.8rem; /* header 總高度 */
}

/* Logo 跨兩行，靠左垂直置中 */
.dhm-logo-img {
    grid-column: 1;
    grid-row: 1 / span 2;
    height: 3rem;
    width: auto;
    object-fit: contain;
    align-self: center;
}

/* 右上：今日菜單 ｜ 會員（靠下對齊，貼近分隔線） */
.dhm-row1 {
    grid-column: 2;
    grid-row: 1;
    display: flex;
    align-items: center; /* 今日菜單 與 會員 垂直置中 */
    justify-content: center;
    gap: 0.5rem;
    padding-bottom: 0.1rem;
    align-self: end;
}
.dhm-title {
    font-size: 1.35rem;
    font-style: italic;
    color: #e3c76b;
    margin: 0;
    line-height: 1;
    letter-spacing: 0.06em;
    white-space: nowrap;
}

/* 手機板 header 內的下拉選單：與會員按鈕等寬，緊貼按鈕 */
.dine-header-mobile .member-dropdown {
    min-width: 100%; /* 與父層 .dh-member 同寬 */
    border-radius: 0.375rem;
    top: calc(100% + 0.05rem); /* 縮短與會員按鈕的距離 */
}
.dine-header-mobile .member-dropdown-item {
    padding: 0.2rem 0.65rem;
    font-size: 0.7rem;
    gap: 0.35rem;
    justify-content: center;
    width: 100%;
}
.dine-header-mobile .member-dropdown-item svg {
    width: 10px;
    height: 10px;
}

/* 手機板 header 內的 +/- 按鈕與人數輸入框：縮小 */
.dine-header-mobile .dh-pax-btn {
    width: 18px;
    height: 18px;
    font-size: 0.85rem;
    border-radius: 0.15rem;
}
.dine-header-mobile .dh-pax-input {
    width: 1.5rem;
    font-size: 0.78rem;
}

/* 手機板 header 內的會員按鈕：縮小，移除固定寬度 */
.dine-header-mobile .dh-member {
    min-width: unset;
    gap: 0.25rem;
    padding: 0.2rem 0.35rem;
}
.dine-header-mobile .dh-member-label {
    font-size: 0.75rem;
    max-width: 5rem;
    overflow: hidden;
    text-overflow: ellipsis;
}
.dine-header-mobile .dh-chevron {
    width: 7px;
    height: 7px;
}

/* 右下：桌號 ｜ 用餐人數（靠上對齊） */
.dhm-row2 {
    grid-column: 2;
    grid-row: 2;
    display: flex;
    align-items: center; /* 桌號、人數控制項垂直置中 */
    gap: 0.35rem;
    padding-top: 0.1rem;
    align-self: start; /* 整行靠上 */
}
.dhm-label {
    font-size: 1rem;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: rgba(208, 197, 181, 0.5);
    white-space: nowrap;
}
.dhm-table-name {
    color: #e3c76b;
    font-size: 0.82rem;
    font-style: italic;
}

/* ════════════════════════════════════════════════════════════════
   ----手機板----   ≤ 1100px  餐點卡片（完整獨立定義，不依賴共用基底）
   ════════════════════════════════════════════════════════════════ */
@media (max-width: 1100px) {
    /* ── 列表視圖卡片 ── */
    .dish-row {
        display: grid;
        grid-template-columns: 110px 1fr;
        align-items: stretch;
        background: #362620;
        border-radius: 0.5rem;
        cursor: pointer;
        transition:
            transform 0.45s cubic-bezier(0.4, 0, 0.2, 1),
            box-shadow 0.45s ease;
    }
    .dish-row:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(0, 0, 0, 0.35);
    }
    .dish-row.dish-row-grid {
        grid-template-columns: 1fr;
        display: flex;
        flex-direction: column;
        overflow: hidden;
    }
    .dish-img {
        width: 110px;
        height: 90px;
        object-fit: cover;
        align-self: center;
        display: block;
        flex-shrink: 0;
        transition: transform 0.7s cubic-bezier(0.4, 0, 0.2, 1);
    }
    .dish-row:hover .dish-img {
        transform: scale(1.05);
    }
    .dish-row-grid .dish-img {
        width: 100%;
        height: 110px;
    }
    .dish-img-placeholder {
        width: 110px;
        height: 90px;
        align-self: center;
        background: #2b1c16;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        color: rgba(208, 197, 181, 0.15);
        font-size: 1.8rem;
    }
    .dish-row-grid .dish-img-placeholder {
        width: 100%;
        height: 110px;
    }
    .dish-content {
        display: flex;
        flex-direction: column;
        justify-content: center;
        padding: 0.5rem 0.6rem;
    }
    .dish-content-grid {
        flex: 1;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: flex-start;
        text-align: center;
    }
    .dish-badges {
        display: flex;
        flex-wrap: wrap;
        gap: 0.3rem;
        margin: 0.4rem 0;
    }
    .dish-content-grid .dish-badges {
        justify-content: center;
    }
    .grid-footer {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 0 0.75rem;
        border-top: 1px solid rgba(77, 70, 58, 0.25);
    }
    .grid-price {
        color: #d5b478;
        font-size: 1rem;
        letter-spacing: 0.08em;
        text-align: center;
    }
    .list-footer {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-top: auto;
        padding-top: 0.4rem;
    }
    .list-footer p {
        margin: 0;
        line-height: 1;
    }
    .qty-col {
        display: flex;
        flex-direction: row;
        align-items: center;
        gap: 0.4rem;
    }
    .qty-row {
        display: flex;
        flex-direction: row;
        align-items: center;
        gap: 0.4rem;
        margin-bottom: 0.8rem;
        margin-top: auto;
    }
    .qty-num {
        color: #f9ddd3;
        width: 30px;
        text-align: center;
    }
    .qty-num.active {
        color: #e3c76b;
    }

    /* ── 網格視圖卡片 ── */
    .dishes-wrap.grid-view {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
        gap: 0.5rem;
    }
    .dish-row-grid .dish-name {
        font-size: 0.95rem;
        text-align: center;
        width: 100%;
        display: -webkit-box;
        -webkit-box-orient: vertical;
        overflow: hidden;
    }

    /* ── 餐點名稱 ── */
    .dish-name {
        font-size: 1rem;
    }

    /* ── 區塊標題 ── */
    .section-title {
        font-family: 'Noto Serif TC', serif;
        font-style: italic;
        font-size: 1.4rem;
        color: #e3c76b;
        padding-top: 0;
        margin-bottom: 0.6rem;
    }

    /* ── 數量按鈕 ── */
    .qty-btn {
        pointer-events: auto !important;
        cursor: pointer !important;
        width: 26px;
        height: 26px;
        border: 1px solid rgba(77, 70, 58, 0.7);
        background: #2b1c16;
        color: #f9ddd3;
        border-radius: 0.125rem;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        font-size: 1.2rem;
        line-height: 1;
        transition:
            border-color 0.3s,
            color 0.3s;
    }
    .qty-btn:hover {
        border-color: #e3c76b;
        color: #e3c76b;
    }
}
</style>
