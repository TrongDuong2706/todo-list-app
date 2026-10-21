<script setup>
import { computed, onMounted, ref } from 'vue'
import { request } from './api'

const todos = ref([])
const title = ref('')
const editId = ref(null)
const editTitle = ref('')
const loading = ref(true)
const busy = ref(false)
const error = ref('')
const completed = computed(() => todos.value.filter(todo => todo.isCompleted).length)

async function perform(action) {
  if (busy.value) return
  busy.value = true
  error.value = ''
  try { await action() }
  catch (err) { error.value = err.message || 'Không kết nối được máy chủ.' }
  finally { busy.value = false }
}

async function load() {
  loading.value = true
  await perform(async () => { todos.value = await request() })
  loading.value = false
}

async function addTodo() {
  if (!title.value.trim()) return
  await perform(async () => {
    const todo = await request('', { method: 'POST', body: JSON.stringify({ title: title.value.trim() }) })
    todos.value.unshift(todo)
    title.value = ''
  })
}

async function update(todo, changes) {
  await perform(async () => {
    const updated = await request(`/${todo.id}`, {
      method: 'PUT', body: JSON.stringify({ title: todo.title, isCompleted: todo.isCompleted, ...changes }),
    })
    todos.value = todos.value.map(item => item.id === updated.id ? updated : item)
    if (editId.value === todo.id) editId.value = null
  })
}

async function remove(todo) {
  if (!window.confirm(`Xóa công việc “${todo.title}”?`)) return
  await perform(async () => {
    await request(`/${todo.id}`, { method: 'DELETE' })
    todos.value = todos.value.filter(item => item.id !== todo.id)
    if (editId.value === todo.id) editId.value = null
  })
}

function startEdit(todo) {
  editId.value = todo.id
  editTitle.value = todo.title
}

onMounted(load)
</script>

<template>
  <main>
    <header>
      <span class="eyebrow">MỖI NGÀY, MỘT CHÚT TIẾN BỘ NHA HUYỀN TRANG</span>
      <h1>Công việc của tôi<span>.</span></h1>
      <p>Ghi lại việc cần làm, dành tâm trí cho điều quan trọng.</p>
    </header>

    <section class="panel" aria-label="Danh sách công việc">
      <form class="add-form" @submit.prevent="addTodo">
        <label class="sr-only" for="new-todo">Công việc mới</label>
        <input id="new-todo" v-model="title" placeholder="Bạn cần làm gì hôm nay?" maxlength="200" required :disabled="busy || loading" />
        <button class="primary" :disabled="busy || loading || !title.trim()">+ Thêm việc</button>
      </form>

      <div class="list-header">
        <h2>Danh sách của bạn</h2>
        <span>{{ completed }}/{{ todos.length }} hoàn thành</span>
      </div>

      <div v-if="error" class="error" role="alert">
        <p>{{ error }}</p>
        <button :disabled="busy" @click="load">Tải lại</button>
      </div>
      <p v-if="loading" class="empty" role="status">Đang tải công việc…</p>
      <p v-else-if="!todos.length && !error" class="empty">Chưa có công việc nào. Thêm việc đầu tiên nhé!</p>

      <ul v-if="!loading">
        <li v-for="todo in todos" :key="todo.id">
          <form v-if="editId === todo.id" class="edit-form" @submit.prevent="update(todo, { title: editTitle.trim() })">
            <label class="sr-only" :for="`edit-${todo.id}`">Sửa tên công việc</label>
            <input :id="`edit-${todo.id}`" v-model="editTitle" maxlength="200" required :disabled="busy" @keydown.esc="editId = null" />
            <button class="primary" :disabled="busy || !editTitle.trim()">Lưu</button>
            <button type="button" :disabled="busy" @click="editId = null">Hủy</button>
          </form>
          <template v-else>
            <button class="check" :class="{ checked: todo.isCompleted }" :aria-label="`${todo.isCompleted ? 'Đánh dấu chưa hoàn thành' : 'Hoàn thành'}: ${todo.title}`" :aria-pressed="todo.isCompleted" :disabled="busy" @click="update(todo, { isCompleted: !todo.isCompleted })">{{ todo.isCompleted ? '✓' : '' }}</button>
            <span class="todo-title" :class="{ done: todo.isCompleted }">{{ todo.title }}</span>
            <div class="actions">
              <button :disabled="busy" :aria-label="`Sửa: ${todo.title}`" @click="startEdit(todo)">Sửa</button>
              <button class="delete" :disabled="busy" :aria-label="`Xóa: ${todo.title}`" @click="remove(todo)">Xóa</button>
            </div>
          </template>
        </li>
      </ul>
      <footer>{{ todos.length - completed }} việc còn lại <span>Chậm rãi. Từng việc một.</span></footer>
    </section>
  </main>
</template>
