const STATUS_URL =
  "http://127.0.0.1:17845/status";

const RULES_URL =
  "http://127.0.0.1:17845/rules";

const STATISTICS_URL =
  "http://127.0.0.1:17845/statistics";

const ALARM_NAME =
  "shieldguard-status-check";

const RULE_ID_OFFSET =
  100000;

let blockedCount = 0;
let protectionActive = false;
let currentRuleIds = [];

async function getStatus() {
  try {
    const response = await fetch(
      STATUS_URL,
      {
        cache: "no-store"
      }
    );

    if (!response.ok) {
      return null;
    }

    return await response.json();
  } catch {
    return null;
  }
}

async function getRules() {
  try {
    const response = await fetch(
      RULES_URL,
      {
        cache: "no-store"
      }
    );

    if (!response.ok) {
      return null;
    }

    return await response.json();
  } catch {
    return null;
  }
}

async function sendStatistics() {
  if (!protectionActive) {
    return;
  }

  try {
    await fetch(
      STATISTICS_URL,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          blockedAds: blockedCount
        })
      }
    );
  } catch {
  }
}

async function removeCurrentRules() {
  if (currentRuleIds.length === 0) {
    return;
  }

  try {
    await chrome.declarativeNetRequest.updateSessionRules({
      removeRuleIds: currentRuleIds
    });
  } catch {
  }

  currentRuleIds = [];
}

async function enableProtection(domains) {
  await removeCurrentRules();

  if (!Array.isArray(domains) ||
      domains.length === 0) {
    protectionActive = true;
    return;
  }

  const rules = [];

  domains.forEach(
    (domain, index) => {
      const normalizedDomain =
        String(domain)
          .trim()
          .toLowerCase();

      if (!normalizedDomain) {
        return;
      }

      rules.push({
        id: RULE_ID_OFFSET + index + 1,
        priority: 1,
        action: {
          type: "block"
        },
        condition: {
          urlFilter:
            `||${normalizedDomain}^`,
          resourceTypes: [
            "main_frame",
            "sub_frame",
            "script",
            "image",
            "xmlhttprequest",
            "media",
            "font",
            "object",
            "ping",
            "other"
          ]
        }
      });
    }
  );

  if (rules.length === 0) {
    protectionActive = true;
    return;
  }

  try {
    await chrome.declarativeNetRequest.updateSessionRules({
      addRules: rules
    });

    currentRuleIds =
      rules.map(rule => rule.id);

    protectionActive = true;
  } catch (error) {
    protectionActive = false;
    console.error(
      "ShieldGuard rule error:",
      error
    );
  }
}

async function disableProtection() {
  await removeCurrentRules();

  protectionActive = false;
  blockedCount = 0;
}

async function synchronize() {
  const status = await getStatus();

  if (!status ||
      status.active !== true) {
    await disableProtection();
    return;
  }

  const rulesResponse =
    await getRules();

  if (!rulesResponse ||
      rulesResponse.active !== true) {
    await disableProtection();
    return;
  }

  await enableProtection(
    rulesResponse.domains
  );

  await sendStatistics();
}

chrome.declarativeNetRequest.onRuleMatchedDebug.addListener(
  async info => {
    if (!protectionActive) {
      return;
    }

    if (
      info &&
      info.rule &&
      info.rule.action &&
      info.rule.action.type === "block"
    ) {
      blockedCount++;

      await sendStatistics();
    }
  }
);

chrome.alarms.onAlarm.addListener(
  async alarm => {
    if (alarm.name !== ALARM_NAME) {
      return;
    }

    await synchronize();
  }
);

chrome.runtime.onInstalled.addListener(
  async () => {
    await chrome.alarms.create(
      ALARM_NAME,
      {
        periodInMinutes: 0.5
      }
    );

    await synchronize();
  }
);

chrome.runtime.onStartup.addListener(
  async () => {
    await chrome.alarms.create(
      ALARM_NAME,
      {
        periodInMinutes: 0.5
      }
    );

    await synchronize();
  }
);

chrome.tabs.onUpdated.addListener(
  async () => {
    if (protectionActive) {
      await sendStatistics();
    }
  }
);

chrome.tabs.onActivated.addListener(
  async () => {
    if (protectionActive) {
      await sendStatistics();
    }
  }
);

chrome.alarms.create(
  ALARM_NAME,
  {
    periodInMinutes: 0.5
  }
);

synchronize();