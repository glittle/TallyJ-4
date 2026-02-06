<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { User, Ticket, Monitor, Suitcase, Connection, UserFilled } from "@element-plus/icons-vue";

const { t } = useI18n();
const router = useRouter();

const options = [
  {
    type: "officer",
    icon: UserFilled,
    color: "#409eff",
    title: "auth.landing.optionOfficer",
    description: "auth.landing.optionOfficerDesc",
    buttonText: "auth.landing.loginOfficer",
    action: () => navigateToLogin("officer"),
  },

  {
    type: "voter",
    icon: Ticket,
    color: "#67c23a",
    title: "auth.landing.optionVoter",
    description: "auth.landing.optionVoterDesc",
    buttonText: "auth.landing.loginVoter",
    action: () => navigateToLogin("voter"),
  },

  {
    type: "teller",
    icon: Monitor,
    color: "#e6a23c",
    title: "auth.landing.optionTeller",
    description: "auth.landing.optionTellerDesc",
    buttonText: "auth.landing.loginTeller",
    action: () => navigateToLogin("teller"),
  },

  {
    type: "full-teller",
    icon: Suitcase,
    color: "#909399",
    title: "auth.landing.optionFullTeller",
    description: "auth.landing.optionFullTellerDesc",
    buttonText: "auth.landing.loginFullTeller",
    action: () => navigateToLogin("full-teller"),
  },
];

const options2 = [
  {
    type: "external",
    icon: Connection,
    color: "#f56c6c",
    title: "auth.landing.optionExternal",
    description: "auth.landing.optionExternalDesc",
    buttonText: "auth.landing.gotoExternal",
    action: () => window.open("https://officers.tallyj.com", "officers"),
  },
];

const navigateToLogin = (type: string) => {
  router.push({ path: "/login", query: { mode: type } });
};
</script>

<template>
  <div class="landing-container">
    <div class="welcome-section">
      <h1>{{ t("auth.landing.title") }}</h1>
      <p class="description">{{ t("auth.landing.description") }}</p>
    </div>

    <div class="options-grid">
      <el-card
        v-for="opt in options"
        :key="opt.type"
        class="option-card"
        shadow="hover"
        @click="opt.action"
      >
        <template #header>
          <div class="card-header">
            <el-icon :size="40" :color="opt.color">
              <component :is="opt.icon" />
            </el-icon>
            <h3>{{ t(opt.title) }}</h3>
          </div>
        </template>
        <p>{{ t(opt.description) }}</p>
        <div class="card-footer">
          <el-button :type="opt.type === 'external' ? 'danger' : 'primary'" plain>
            {{ t(opt.buttonText) }}
          </el-button>
        </div>
      </el-card>
    </div>
    <div class="options-grid2">
      <el-card
        v-for="opt in options2"
        :key="opt.type"
        class="option-card"
        shadow="hover"
        @click="opt.action"
      >
        <template #header>
          <div class="card-header">
            <el-icon :size="40" :color="opt.color">
              <component :is="opt.icon" />
            </el-icon>
            <h3>{{ t(opt.title) }}</h3>
          </div>
        </template>
        <p>{{ t(opt.description) }}</p>
        <div class="card-footer">
          <el-button :type="opt.type === 'external' ? 'danger' : 'primary'" plain>
            {{ t(opt.buttonText) }}
          </el-button>
        </div>
      </el-card>
    </div>
  </div>
</template>

<style lang="less">
.landing-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 40px 20px;
  color: white;

  .welcome-section {
    text-align: center;
    margin-bottom: 60px;
  }

  .welcome-section h1 {
    font-size: 3rem;
    margin-bottom: 20px;
    text-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
  }

  .description {
    font-size: 1.2rem;
    line-height: 1.6;
    max-width: 800px;
    margin: 0 auto;
    opacity: 0.9;
  }

  .options-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 20px;

    .option-card {
      cursor: pointer;
      transition: transform 0.3s ease;
      text-align: center;
      border-radius: 12px;
      border: none;
      background: rgba(255, 255, 255, 0.9);
      display: flex;
      flex-direction: column;
    }
  }

  .options-grid2 {
    margin: 1.5em auto 0;
    width: fit-content;

    .option-card {
      cursor: pointer;
      transition: transform 0.3s ease;
      text-align: center;
      border-radius: 12px;
      border: none;
      background: rgba(255, 255, 255, 0.9);
      display: flex;
      flex-direction: column;
    }
  }

  .option-card:hover {
    transform: translateY(-10px);
  }

  .card-header {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 15px;
  }

  .card-header h3 {
    margin: 0;
    color: #303133;
    font-size: 1.3rem;
  }

  .option-card p {
    color: #606266;
    min-height: 60px;
    font-size: 0.95rem;
  }

  .card-footer {
    margin-top: auto;
    padding-top: 20px;
  }

  .el-button {
    width: 100%;
    font-weight: 600;
  }
}
</style>
