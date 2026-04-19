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
  WarnTriangleFilled,
} from "@element-plus/icons-vue";
import { ElIcon } from "element-plus";
import { onMounted, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";

const { t } = useI18n();
const router = useRouter();

const options = [
  {
    type: "voter",
    icon: Ticket,
    // color: "#F47920",
    color: "#E2725B",
    title: "auth.landing.optionVoter",
    description: "auth.landing.optionVoterDesc",
    buttonText: "auth.landing.loginVoter",
    action: () => router.push({ name: "voter-auth" }),
  },

  {
    type: "teller",
    icon: Monitor,
    // color: "#8DC63F",
    color: "#006D77",
    title: "auth.landing.optionTeller",
    description: "auth.landing.optionTellerDesc",
    buttonText: "auth.landing.loginTeller",
    action: () => router.push({ name: "teller-join" }),
  },
  {
    type: "officer",
    icon: UserFilled,
    // color: "#2563a8",
    color: "#1D3557",
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
    // color: "#F47920",
    color: "#DAA520",
    title: "auth.landing.optionExternal",
    description: "auth.landing.optionExternalDesc",
    buttonText: "auth.landing.gotoExternal",
    action: () => window.open("https://officers.tallyj.com", "officers"),
  },
];

const features = [
  {
    icon: Clock,
    color: "#8DC63F",
    // color: "#2563a8",
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
    color: "#8DC63F",
    // color: "#F47920",
    title: "auth.landing.features.flexible.title",
    description: "auth.landing.features.flexible.description",
  },
  {
    icon: Document,
    color: "#8DC63F",
    // color: "#1C3A6A",
    title: "auth.landing.features.transparent.title",
    description: "auth.landing.features.transparent.description",
  },
  {
    icon: Location,
    color: "#8DC63F",
    //color: "#2563a8",
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
function handleKeydown(event: KeyboardEvent) {
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
}

onMounted(() => {
  globalThis.addEventListener("keydown", handleKeydown);
});

onUnmounted(() => {
  globalThis.removeEventListener("keydown", handleKeydown);
});
</script>

<template>
  <div class="landing-container">
    <div class="welcome-section">
      <h1>{{ t("auth.landing.title") }}</h1>

      <i18n-t keypath="common.testOnlyLong" tag="div" class="testOnlyWarning">
        <template #note>
          <el-icon>
            <WarnTriangleFilled />
          </el-icon>
          <strong>{{ $t("common.testOnlyLongNote") }}</strong>
        </template>
        <template #br><br /></template>
      </i18n-t>

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
            <el-icon :size="48" :color="opt.color">
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
    <div class="secondary-action-row">
      <div v-for="opt in options2" :key="opt.type">
        <p>
          <span>{{ t(opt.description) }}</span>
          <a
            href="https://officers.tallyj.com/"
            class="secondary-link"
            target="_officers"
            rel="noopener noreferrer"
          >
            <span> {{ t(opt.title) }}</span>
            <!-- Inline SVG for the 'External Link' icon -->
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              roke-linecap="round"
              stroke-linejoin="round"
              class="icon-external"
            >
              <path
                d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"
              ></path>
              <polyline points="15 3 21 3 21 9"></polyline>
              <line x1="10" y1="14" x2="21" y2="3"></line>
            </svg>
          </a>
        </p>
      </div>
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
  margin: 0 auto;
  max-width: var(--normal-max-width);
  color: var(--el-text-color-primary);

  .welcome-section {
    text-align: center;
  }

  .welcome-section h1 {
    font-family: "Lora", serif;
    font-size: 3.4rem;
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

  .option-card {
    flex: 1 1 300px; /* Allows cards to grow/shrink but not get too small */
    max-width: 400px;

    cursor: pointer;
    box-shadow:
      0 10px 30px rgba(0, 0, 0, 0.05),
      0 1px 8px rgba(0, 0, 0, 0.02);
    transition:
      transform 0.3s ease,
      box-shadow 0.3s ease;
    text-align: center;
    border-radius: 24px;
    border: none;
    background: var(--el-bg-color);
    display: flex;
    flex-direction: column;

    .el-icon {
      border-radius: 50%;
      padding: 12px;
      /* Create a soft "tint" of the brand color */
      background-color: color-mix(in srgb, var(--color), transparent 90%);
    }
  }

  /* The Container for your 3 core cards */
  .options-grid {
    display: flex;
    justify-content: center; /* Centers the 3 cards horizontally */
    gap: 32px; /* Adds breathing room between cards */
    flex-wrap: wrap; /* Ensures responsiveness on smaller screens */
    padding: 40px 20px;
    max-width: 1440px; /* As we discussed earlier */
    margin: 0 auto 3em;
  }

  /* The Secondary Action Row */
  .secondary-action-row {
    justify-content: center; /* Centers the link horizontally */
    background-color: color-mix(
      in srgb,
      var(--color-navy),
      transparent 95%
    ); /* Very subtle tint */
    border-radius: 100px; /* Creates a "pill" or capsule shape */
    width: fit-content;
    margin: 0 auto; /* Centers the row itself on the page */

    p {
      display: flex;
      align-items: center;
      gap: 2em;
      padding: 12px 36px;
      font-size: 1rem;
      color: var(--el-text-color-primary);
      border-radius: 30px;
      background-color: #2562a814; /* 10% opacity of the feature color */
    }

    .secondary-link {
      display: flex;
      gap: 0.25em;
      align-items: center;
      &:hover {
        text-decoration: underline;
      }
    }
  }

  .option-card:hover,
  .option-card:focus {
    transform: scale(1.02);
    box-shadow: 0 15px 45px rgba(0, 0, 0, 0.08);
    .el-button {
      background-color: var(--el-button-hover-bg-color) !important;
      box-shadow: 0 6px 16px -2px rgba(226, 114, 91, 0.5);
    }
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
    font-size: 1.6rem;
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
    border-radius: 30px;
    padding: 20px 32px;
    height: auto;
    width: 100%;
    font-weight: 600;

    /* Use the variable as the base, but layer a light linear gradient over it */
    background-image: linear-gradient(
      180deg,
      rgba(255, 255, 255, 0.15) 0%,
      rgba(0, 0, 0, 0.05) 100%
    ) !important;
    background-color: var(--el-button-bg-color) !important;

    /* Add depth using the background variable for a tinted shadow */
    box-shadow: 0 4px 12px -2px rgba(226, 114, 91, 0.4);
  }

  // Marketing Benefits Section
  .benefits-section {
    margin-top: 80px;
    padding: 60px 0;
    animation: fadeIn 1s ease-in 0.4s both;
  }

  .benefits-header {
    text-align: center;
    margin-bottom: 2em;
  }

  .benefits-header h2 {
    font-size: 2.5rem;
    margin-bottom: 20px;
    color: #006d77;
    color: var(--color-public-header-text);
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

    .feature-icon {
      margin-bottom: 20px;
      display: flex;
      justify-content: center;
      align-items: center;
      height: 60px;
      .el-icon {
        border-radius: 50%;
        padding: 12px;
        background-color: #2562a814; /* 10% opacity of the feature color */
      }
    }

    h3 {
      font-size: 1.2rem;
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
    padding: 1.25rem 2rem;
    margin: 1.5rem auto;
    max-width: 920px; /* Comfortable reading width */
    text-align: center;
    background-color: #fff4e5;
    color: #8c4a00;
    font-size: 1.05rem;
    line-height: 1.5;
    border: 2px solid #f5a23d;
    border-radius: 10px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    font-weight: 500;
    i {
      margin-right: 0.25em;
      top: 2px;
    }
    strong {
      margin-right: 0.25em;
    }
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

  // Responsive Design
  @media (max-width: 768px) {
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
:root.dark {
  .landing-container {
    .testOnlyWarning {
      background-color: #3f2a1a;
      color: #ffd9a8;
      border-color: #f5a23d;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25);
    }
  }
}
</style>
