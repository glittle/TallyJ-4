<script setup lang="ts"></script>

<template>
  <h1>TallyJ 4!</h1>
  <p><strong>Current route path:</strong> {{ $route.fullPath }}</p>
  <nav>
    <RouterLink to="/">Go to Home</RouterLink>
    <RouterLink to="/about">Go to About</RouterLink>
  </nav>
  <main>
    <RouterView />
  </main>
</template>

<style lang="less">
h1 {
  font-weight: normal;
}
</style>
