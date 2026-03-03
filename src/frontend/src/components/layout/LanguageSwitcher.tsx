import { useTranslation } from 'react-i18next';

const LANGUAGES = [
  { code: 'de', label: 'DE' },
  { code: 'en', label: 'EN' },
  { code: 'ru', label: 'RU' },
] as const;

export default function LanguageSwitcher() {
  const { i18n } = useTranslation();

  return (
    <div className="flex items-center gap-0.5 rounded-md border border-border bg-secondary px-1 py-0.5">
      {LANGUAGES.map(({ code, label }, index) => (
        <button
          key={code}
          onClick={() => i18n.changeLanguage(code)}
          className={`px-1.5 py-0.5 text-xs font-medium rounded transition-colors ${
            i18n.language === code
              ? 'text-primary'
              : 'text-muted-foreground hover:text-foreground'
          }${index < LANGUAGES.length - 1 ? ' border-r border-border' : ''}`}
        >
          {label}
        </button>
      ))}
    </div>
  );
}
