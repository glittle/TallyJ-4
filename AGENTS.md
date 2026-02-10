No need to run "npm run build" after front end changes - the developer has it running.
Vue files should have elements in this order: <script setup lang="ts"/><template/><style lang="less"/>. Do not use <style scoped> but nest all CSS content inside the component's root element CSS.
