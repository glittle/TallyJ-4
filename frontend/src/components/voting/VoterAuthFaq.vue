<script setup lang="ts">
import { QuestionFilled } from "@element-plus/icons-vue";
import { ElCollapse, ElCollapseItem, ElIcon } from "element-plus";
</script>

<template>
  <div class="voter-auth-faq">
    <div class="faq-header">
      <ElIcon :size="24" color="var(--color-public-text)">
        <QuestionFilled />
      </ElIcon>
      <h2>{{ $t("voting.auth.faq.title") }}</h2>
    </div>
    <ElCollapse class="faq-collapse">
      <ElCollapseItem :title="$t('voting.auth.faq.q1')" name="1">
        {{ $t("voting.auth.faq.a1") }}
      </ElCollapseItem>
      <ElCollapseItem :title="$t('voting.auth.faq.q2')" name="2">
        {{ $t("voting.auth.faq.a2") }}
      </ElCollapseItem>
      <ElCollapseItem :title="$t('voting.auth.faq.q3')" name="3">
        {{ $t("voting.auth.faq.a3") }}
      </ElCollapseItem>
      <ElCollapseItem :title="$t('voting.auth.faq.q4')" name="4">
        {{ $t("voting.auth.faq.a4") }}
      </ElCollapseItem>
      <ElCollapseItem :title="$t('voting.auth.faq.q5')" name="5">
        {{ $t("voting.auth.faq.a5") }}
      </ElCollapseItem>
      <ElCollapseItem :title="$t('voting.auth.faq.q6')" name="6">
        {{ $t("voting.auth.faq.a6") }}
      </ElCollapseItem>
      <ElCollapseItem :title="$t('voting.auth.faq.q7')" name="7">
        {{ $t("voting.auth.faq.a7") }}
      </ElCollapseItem>
    </ElCollapse>
  </div>
</template>

<style lang="less">
.voter-auth-faq {
  margin-top: 32px;

  .faq-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 12px;
    text-align: start;

    h2 {
      margin: 0;
      font-size: 1.25rem;
      color: var(--color-public-text);
    }
  }

  .faq-collapse {
    background: transparent;
    text-align: start;

    .el-collapse-item__header {
      text-align: start;
      justify-content: flex-start;
    }

    /* Element Plus uses physical `margin-left: auto` on the arrow */
    .el-collapse-item__arrow {
      margin-block: 0;
      margin-inline-start: auto;
      margin-inline-end: 8px;
    }

    .el-collapse-item__content {
      text-align: start;
    }
  }
}
</style>
