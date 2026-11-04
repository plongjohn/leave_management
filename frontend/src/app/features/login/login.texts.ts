export type LoginFailure = 'invalidCredentials' | 'tooManyAttempts' | 'unknown';

export const loginTexts = {
  pageTitle: 'Anmelden – Urlaubsverwaltung',
  appName: 'Urlaubsverwaltung',
  subtitle: 'Melde dich mit deiner Firmen-E-Mail an',
  heroTitle: 'Zeit für Erholung.',
  heroSubtitle: 'Urlaub beantragen, Freigaben erteilen und den Überblick im Team behalten.',
  email: 'E-Mail',
  emailPlaceholder: 'vorname.nachname@firma.at',
  emailRequired: 'Bitte gib deine E-Mail-Adresse ein.',
  emailInvalid: 'Das ist keine gültige E-Mail-Adresse.',
  password: 'Passwort',
  passwordRequired: 'Bitte gib dein Passwort ein.',
  showPassword: 'Passwort anzeigen',
  hidePassword: 'Passwort verbergen',
  rememberMe: 'Angemeldet bleiben',
  forgotPassword: 'Passwort vergessen?',
  forgotPasswordHint: 'Bitte wende dich an die Personalabteilung – sie setzt dein Passwort zurück.',
  submit: 'Anmelden',
  submitting: 'Anmeldung läuft …',
  noAccount: 'Noch kein Zugang? Die Personalabteilung legt dein Konto an.',
  failures: {
    invalidCredentials:
      'E-Mail oder Passwort ist falsch, oder das Konto ist vorübergehend gesperrt.',
    tooManyAttempts: 'Zu viele Anmeldeversuche. Bitte warte eine Minute und versuche es erneut.',
    unknown: 'Die Anmeldung ist gerade nicht möglich. Bitte versuche es später erneut.',
  } satisfies Record<LoginFailure, string>,
} as const;
