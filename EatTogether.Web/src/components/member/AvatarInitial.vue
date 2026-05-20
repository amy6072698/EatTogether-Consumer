<template>
    <div class="avatar-wrap" :class="[`avatar-wrap--${displayMode}`, { 'avatar-wrap--interactive': interactive }]" :style="sizeStyle">
        <!-- 模式 1：上傳頭像 -->
        <img v-if="displayMode === 'photo'" :src="apiOrigin + '/uploads/avatars/' + avatarFileName" alt="使用者頭像" class="avatar-photo" />

        <!-- 模式 1.5：Google 頭像 -->
        <img v-else-if="displayMode === 'google'" :src="googleAvatarUrl" alt="Google 頭像" class="avatar-photo" />

        <!-- 模式 2：預設圖示 -->
        <img v-else-if="!avatarLoadError" :src="defaultAvatar" alt="預設頭像" class="avatar-default" @error="avatarLoadError = true" />

        <!-- 模式 3：名字首字 -->
        <div v-else class="avatar-initial" :style="fontStyle">
            {{ initial }}
        </div>
    </div>
</template>

<!--
  AvatarInitial.vue — 統一頭像顯示元件
  依優先順序自動選擇顯示模式，外部不需判斷，直接傳入 props 即可。

  顯示優先順序：
    1. avatarFileName 有值 → 上傳頭像圖片
    2. avatarFileName 為 null  → 預設頭像圖示（default-avatar.svg）
    3. 預設頭像圖示錯誤 name 有值 → 名字第一個字（金色圓形背景）

  Props:
    avatarFileName (String|null, default null) — 後端回傳的頭像檔名
    name           (String, default '')        — 會員名稱，取第一個字
    size           (String, default '36px')    — 圓形直徑
    interactive    (Boolean, default false)    — true 時顯示 hover 金色邊框與 cursor: pointer
                                                 用於可點擊的頭像區塊（如 Navbar）

  使用範例:
    使用範例:
    <AvatarInitial :avatarFileName="member.avatarFileName" :name="member.name" />
    <AvatarInitial :avatarFileName="member.avatarFileName" :name="member.name" size="80px" />
    <AvatarInitial interactive />    ← Navbar 可點擊頭像，帶 hover 效果
    <AvatarInitial />                ← 純顯示用，無 hover 效果
-->
<script setup>
import { computed, ref } from 'vue'
import defaultAvatar from '@/assets/images/default-avatar.svg'

const avatarLoadError = ref(false)

const apiOrigin = import.meta.env.VITE_API_BASE_URL?.replace('/api', '') ?? ''

const props = defineProps({
    avatarFileName: { type: String, default: null },
    googleAvatarUrl: { type: String, default: null },
    name: { type: String, default: '' },
    size: { type: String, default: '36px' },
    interactive: { type: Boolean, default: false },
})

const displayMode = computed(() => {
    if (props.avatarFileName) return 'photo'
    if (props.googleAvatarUrl) return 'google'
    return 'default'
})

const initial = computed(() => props.name?.[0] ?? '')

const sizeStyle = computed(() => ({
    width: props.size,
    height: props.size,
}))

const fontStyle = computed(() => ({
    fontSize: `calc(${props.size} * 0.4)`,
}))
</script>

<style scoped>
.avatar-wrap {
    border-radius: 50%;
    overflow: hidden;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
}

/* 模式 1：上傳頭像 */
.avatar-photo {
    width: 100%;
    height: 100%;
    object-fit: cover;
    display: block;
    border-radius: 50%;
    border: 2px solid transparent;
}

/* 模式 2：預設圖示（背景色保留，border 移交 interactive 控制）*/
.avatar-wrap--default {
    background-color: rgba(226, 210, 185, 0.15);
}

/* 模式 3：名字首字 */
.avatar-initial {
    width: 100%;
    height: 100%;
    background-color: var(--eat-primary-container);
    color: var(--eat-on-primary);
    font-family: var(--font-label);
    font-weight: 600;
    display: flex;
    align-items: center;
    justify-content: center;
    user-select: none;
    line-height: 1;
}

/* Interactive（可點擊頭像）hover 效果 */
.avatar-wrap--interactive {
    border: 1px solid rgba(245, 216, 122, 0.3);
    cursor: pointer;
    transition: border-color 0.3s ease;
}

.avatar-wrap--interactive:hover {
    border-color: var(--eat-primary);
}

.avatar-default {
    width: 100%;
    height: 100%;
    filter: brightness(0) invert(0.6);
}
</style>
