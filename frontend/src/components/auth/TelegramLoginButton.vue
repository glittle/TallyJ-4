<script setup lang="ts">
import { onBeforeUnmount, onMounted } from "vue";

type RequestAccess = "write" | "read";
type ButtonSize = "large" | "medium" | "small";

const props = defineProps<{
  botUsername: string;
  requestAccess?: RequestAccess;
  size?: ButtonSize;
  radius?: string;
}>();

const emit = defineEmits<{
  (e: "success", user: any): void;
}>();

const scriptEl = document.createElement("script");

onMounted(() => {
  if (!props.botUsername) {
    return;
  }

  (window as any).onTelegramAuth = (user: any) => {
    emit("success", user);
  };

  scriptEl.src = "https://telegram.org/js/telegram-widget.js?22";
  scriptEl.async = true;
  scriptEl.setAttribute("data-telegram-login", props.botUsername);
  scriptEl.setAttribute("data-size", props.size ?? "large");
  scriptEl.setAttribute("data-radius", props.radius ?? "8");
  scriptEl.setAttribute("data-request-access", props.requestAccess ?? "write");
  scriptEl.setAttribute("data-onauth", "onTelegramAuth(user)");
  scriptEl.setAttribute("data-userpic", "false");
  const container = document.getElementById("telegram-login-container");
  if (container) {
    container.innerHTML = "";
    container.appendChild(scriptEl);
  } else {
    document.body.appendChild(scriptEl);
  }
});

onBeforeUnmount(() => {
  if (scriptEl.parentElement) {
    scriptEl.parentElement.removeChild(scriptEl);
  }
  if ((window as any).onTelegramAuth) {
    delete (window as any).onTelegramAuth;
  }
});
</script>

<template>
  <div id="telegram-login-container" class="telegram-login-container" />
</template>

<style lang="less">
.telegram-login-container {
  display: flex;
  justify-content: center;
}
</style>
