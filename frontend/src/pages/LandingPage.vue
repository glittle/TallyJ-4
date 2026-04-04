<script setup lang="ts">
import {
  Check,
  Clock,
  Connection,
  Document,
  Location,
  Lock,
  Monitor,
  Pointer,
  Ticket,
  UserFilled,
} from "@element-plus/icons-vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";

const { t } = useI18n();
const router = useRouter();

const options = [
  {
    type: "voter",
    icon: Ticket,
    color: "#F47920",
    title: "auth.landing.optionVoter",
    description: "auth.landing.optionVoterDesc",
    buttonText: "auth.landing.loginVoter",
    action: () => router.push({ name: "voter-auth" }),
  },

  {
    type: "teller",
    icon: Monitor,
    color: "#8DC63F",
    title: "auth.landing.optionTeller",
    description: "auth.landing.optionTellerDesc",
    buttonText: "auth.landing.loginTeller",
    action: () => router.push({ name: "teller-join" }),
  },
  {
    type: "officer",
    icon: UserFilled,
    color: "#2563a8",
    title: "auth.landing.optionOfficer",
    description: "auth.landing.optionOfficerDesc",
    buttonText: "auth.landing.loginOfficer",
    action: () => navigateToLogin("officer"),
  },
];

const options2 = [
  {
    type: "external",
    icon: Connection,
    color: "#F47920",
    title: "auth.landing.optionExternal",
    description: "auth.landing.optionExternalDesc",
    buttonText: "auth.landing.gotoExternal",
    action: () => window.open("https://officers.tallyj.com", "officers"),
  },
];

const features = [
  {
    icon: Clock,
    color: "#2563a8",
    title: "auth.landing.features.realtime.title",
    description: "auth.landing.features.realtime.description",
  },
  {
    icon: Lock,
    color: "#8DC63F",
    title: "auth.landing.features.secure.title",
    description: "auth.landing.features.secure.description",
  },
  {
    icon: Pointer,
    color: "#F47920",
    title: "auth.landing.features.flexible.title",
    description: "auth.landing.features.flexible.description",
  },
  {
    icon: Document,
    color: "#1C3A6A",
    title: "auth.landing.features.transparent.title",
    description: "auth.landing.features.transparent.description",
  },
  {
    icon: Location,
    color: "#2563a8",
    title: "auth.landing.features.multilingual.title",
    description: "auth.landing.features.multilingual.description",
  },
  {
    icon: Check,
    color: "#8DC63F",
    title: "auth.landing.features.accessible.title",
    description: "auth.landing.features.accessible.description",
  },
];

const navigateToLogin = (type: string) => {
  if (type === "voter") {
    router.push({ name: "voter-auth" });
  } else {
    router.push({ path: "/login", query: { mode: type } });
  }
};

// add keyboard shortcut: if user presses "v", go to voter login; if "t", teller login; if "o", officer login
// use the first letter of the type as the shortcut
globalThis.addEventListener("keydown", (event) => {
  if (
    event.target instanceof HTMLInputElement ||
    event.target instanceof HTMLTextAreaElement
  ) {
    // Don't trigger shortcuts when typing in input fields
    return;
  }

  // find the options that matches the pressed key and navigate to the corresponding login page
  const matched = options.find(
    (opt) =>
      t(opt.title)[0]?.toLocaleLowerCase() === event.key.toLocaleLowerCase(),
  );
  if (matched) {
    matched.action();
  }
});
</script>

<template>
  <div class="landing-container">
    <div class="testOnlyWarning">
      {{ $t("common.testOnlyLong") }}
    </div>
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
        tabindex="0"
        :style="{ '--card-accent-color': opt.color }"
        @click="opt.action"
        @keydown.enter="opt.action"
        @keydown.space.prevent="opt.action"
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
          <el-button :color="opt.color">
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
        tabindex="0"
        :style="{ '--card-accent-color': opt.color }"
        @click="opt.action"
        @keydown.enter="opt.action"
        @keydown.space.prevent="opt.action"
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
          <el-button :color="opt.color">
            {{ t(opt.buttonText) }}
          </el-button>
        </div>
      </el-card>
    </div>

    <!-- Marketing Benefits Section -->
    <div class="benefits-section">
      <div class="benefits-header">
        <h2>{{ t("auth.landing.benefits.title") }}</h2>
        <p class="benefits-subtitle">
          {{ t("auth.landing.benefits.subtitle") }}
        </p>
      </div>

      <div class="features-grid">
        <div
          v-for="feature in features"
          :key="feature.title"
          class="feature-card"
        >
          <div class="feature-icon">
            <el-icon :size="32" :color="feature.color">
              <component :is="feature.icon" />
            </el-icon>
          </div>
          <h3>{{ t(feature.title) }}</h3>
          <p>{{ t(feature.description) }}</p>
        </div>
      </div>

      <div class="cta-section">
        <h3>{{ t("auth.landing.cta.title") }}</h3>
        <p>{{ t("auth.landing.cta.description") }}</p>
      </div>
    </div>

    <div class="statusDocLink">
      <a
        href="https://docs.google.com/document/d/1WXrVy2Jl3Lk-Vs1k77t2QnrrdK5zjzSeHMXnOx0Deao/edit?usp=sharing"
        target="statusDoc"
        >Status & Feedback Document V4 (English only)</a
      >
    </div>
  </div>
</template>

<style lang="less">
.landing-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 10px;
  color: var(--el-text-color-primary);

  .welcome-section {
    text-align: center;
    margin-bottom: 60px;
    animation: fadeIn 0.8s ease-in;
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
    animation: slideUp 0.8s ease-out 0.2s both;

    .option-card {
      cursor: pointer;
      transition:
        transform 0.3s ease,
        box-shadow 0.3s ease;
      text-align: center;
      border-radius: 12px;
      border: none;
      background: var(--el-bg-color);
      display: flex;
      flex-direction: column;
    }
  }

  .options-grid2 {
    margin: 1.5em auto 0;
    width: fit-content;
    animation: slideUp 0.8s ease-out 0.3s both;

    .option-card {
      cursor: pointer;
      transition:
        transform 0.3s ease,
        box-shadow 0.3s ease;
      text-align: center;
      border-radius: 12px;
      border: none;
      background: var(--el-bg-color);
      display: flex;
      flex-direction: column;
    }
  }

  .option-card:hover,
  .option-card:focus {
    transform: translateY(-10px);
    box-shadow: 0 12px 24px rgba(0, 0, 0, 0.15);
  }

  .option-card::before {
    background: var(--card-accent-color);
  }

  .card-header {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 15px;
  }

  .card-header h3 {
    margin: 0;
    color: var(--el-text-color-primary);
    font-size: 1.3rem;
  }

  .option-card p {
    color: var(--el-text-color-regular);
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

  // Marketing Benefits Section
  .benefits-section {
    margin-top: 80px;
    padding: 60px 0;
    animation: fadeIn 1s ease-in 0.4s both;
  }

  .benefits-header {
    text-align: center;
    margin-bottom: 60px;
  }

  .benefits-header h2 {
    font-size: 2.5rem;
    margin-bottom: 20px;
    color: var(--color-public-text);
  }

  .benefits-subtitle {
    font-size: 1.1rem;
    line-height: 1.7;
    max-width: 700px;
    margin: 0 auto;
    opacity: 0.85;
  }

  .features-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 30px;
    margin-bottom: 60px;
  }

  .feature-card {
    background: var(--el-bg-color);
    border-radius: 16px;
    padding: 30px;
    text-align: center;
    transition: all 0.3s ease;
    border: 1px solid var(--el-border-color);

    // &:hover {
    // background: var(--el-bg-color-page);
    // transform: translateY(-5px);
    // border-color: var(--el-border-color-darker);
    // }

    .feature-icon {
      margin-bottom: 20px;
      display: flex;
      justify-content: center;
      align-items: center;
      height: 60px;
    }

    h3 {
      font-size: 1.3rem;
      margin-bottom: 15px;
      color: var(--el-text-color-primary);
    }

    p {
      color: var(--el-text-color-regular);
      line-height: 1.6;
      font-size: 0.95rem;
    }
  }

  .cta-section {
    text-align: center;
    padding: 40px;
    background: var(--el-bg-color);
    border-radius: 20px;
    border: 1px solid var(--el-border-color);

    h3 {
      font-size: 1.8rem;
      margin-bottom: 15px;
      color: var(--el-text-color-primary);
    }

    p {
      font-size: 1.1rem;
      color: var(--el-text-color-regular);
      max-width: 600px;
      margin: 0 auto;
    }
  }

  .statusDocLink {
    text-align: center;
    margin-bottom: 40px;
    font-size: 1.15em;

    a {
      color: var(--el-link-color);
      font-weight: 500;
      text-decoration: none;
      border-bottom: 2px solid transparent;
      transition: border-color 0.3s ease;

      &:hover {
        border-color: var(--el-link-color);
      }
    }
  }

  .testOnlyWarning {
    padding: 0.25em 0.5em;
    text-align: center;
    margin-left: 5%;
    background-color: var(--el-color-error);
    color: var(--color-error-50);
    font-size: 1.3em;
    font-weight: bold;
    border-radius: 10px;
    width: fit-content;
    transform: rotate(-5deg);
    animation: pulse-ltr 3s ease-in-out infinite;
  }

  // Animations
  @keyframes fadeIn {
    from {
      opacity: 0;
    }

    to {
      opacity: 1;
    }
  }

  @keyframes pulse-ltr {
    0%,
    100% {
      transform: rotate(-5deg) scale(1);
    }
    50% {
      transform: rotate(-5deg) scale(1.05);
    }
  }

  @keyframes slideUp {
    from {
      opacity: 0;
      transform: translateY(30px);
    }

    to {
      opacity: 1;
      transform: translateY(0);
    }
  }

  // Responsive Design
  @media (max-width: 768px) {
    padding: 20px 15px;

    .welcome-section h1 {
      font-size: 2rem;
    }

    .description {
      font-size: 1rem;
    }

    .benefits-header h2 {
      font-size: 1.8rem;
    }

    .benefits-subtitle {
      font-size: 1rem;
    }

    .features-grid {
      grid-template-columns: 1fr;
      gap: 20px;
    }

    .options-grid {
      grid-template-columns: 1fr;
    }

    .cta-section h3 {
      font-size: 1.5rem;
    }

    .cta-section p {
      font-size: 1rem;
    }
  }
}

@keyframes pulse-rtl {
  0%,
  100% {
    transform: rotate(5deg) scale(1);
  }
  50% {
    transform: rotate(5deg) scale(1.05);
  }
}

:lang(ar),
:lang(fa) {
  .landing-container {
    .testOnlyWarning {
      animation: pulse-rtl 3s ease-in-out infinite;
    }
  }
}
</style>
