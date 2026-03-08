<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { Ticket, Monitor, Connection, UserFilled, Clock, Lock, Pointer, Document, Location, Check } from "@element-plus/icons-vue";

const { t } = useI18n();
const router = useRouter();

const options = [
  {
    type: "voter",
    icon: Ticket,
    color: "#8DC63F",
    title: "auth.landing.optionVoter",
    description: "auth.landing.optionVoterDesc",
    buttonText: "auth.landing.loginVoter",
    action: () => router.push({ name: "voter-auth" }),
  },

  {
    type: "teller",
    icon: Monitor,
    color: "#F47920",
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
  if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
    // Don't trigger shortcuts when typing in input fields
    return;
  }

  // find the options that matches the pressed key and navigate to the corresponding login page
  const matched = options.find(opt => t(opt.title)[0]?.toLocaleLowerCase() === event.key.toLocaleLowerCase());
  if (matched) {
    matched.action();
  }
});

</script>

<template>
  <div class="landing-container">
    <div class="welcome-section">
      <h1>{{ t("auth.landing.title") }}</h1>
      <p class="description">{{ t("auth.landing.description") }}</p>
    </div>

    <div class="options-grid">
      <el-card v-for="opt in options" :key="opt.type" class="option-card" shadow="hover" tabindex="0"
        @click="opt.action" @keydown.enter="opt.action" @keydown.space.prevent="opt.action">
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
      <el-card v-for="opt in options2" :key="opt.type" class="option-card" shadow="hover" tabindex="0"
        @click="opt.action" @keydown.enter="opt.action" @keydown.space.prevent="opt.action">
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

    <!-- Marketing Benefits Section -->
    <div class="benefits-section">
      <div class="benefits-header">
        <h2>{{ t("auth.landing.benefits.title") }}</h2>
        <p class="benefits-subtitle">{{ t("auth.landing.benefits.subtitle") }}</p>
      </div>

      <div class="features-grid">
        <div v-for="feature in features" :key="feature.title" class="feature-card">
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
      transition: transform 0.3s ease, box-shadow 0.3s ease;
      text-align: center;
      border-radius: 12px;
      border: none;
      background: rgba(255, 255, 255, 0.95);
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
      transition: transform 0.3s ease, box-shadow 0.3s ease;
      text-align: center;
      border-radius: 12px;
      border: none;
      background: rgba(255, 255, 255, 0.95);
      display: flex;
      flex-direction: column;
    }
  }

  .option-card:hover,
  .option-card:focus {
    transform: translateY(-10px);
    box-shadow: 0 12px 24px rgba(0, 0, 0, 0.15);
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

    .el-button {
      --el-button-text-color: white;
    }
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
    color: white;

    @supports (background-clip: text) or (-webkit-background-clip: text) {
      background: linear-gradient(135deg, #ffffff 0%, #e0e7ff 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }
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
    background: rgba(255, 255, 255, 0.1);
    backdrop-filter: blur(10px);
    border-radius: 16px;
    padding: 30px;
    text-align: center;
    transition: all 0.3s ease;
    border: 1px solid rgba(255, 255, 255, 0.2);

    &:hover {
      background: rgba(255, 255, 255, 0.15);
      transform: translateY(-5px);
      border-color: rgba(255, 255, 255, 0.3);
    }

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
      color: white;
    }

    p {
      color: rgba(255, 255, 255, 0.85);
      line-height: 1.6;
      font-size: 0.95rem;
    }
  }

  .cta-section {
    text-align: center;
    padding: 40px;
    background: rgba(255, 255, 255, 0.08);
    backdrop-filter: blur(10px);
    border-radius: 20px;
    border: 1px solid rgba(255, 255, 255, 0.15);

    h3 {
      font-size: 1.8rem;
      margin-bottom: 15px;
      color: white;
    }

    p {
      font-size: 1.1rem;
      opacity: 0.85;
      max-width: 600px;
      margin: 0 auto;
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
</style>
