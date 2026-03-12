import type { Account } from '@/types/accounting';

export function getLocalizedAccountName(
  account: Pick<Account, 'name' | 'nameDe' | 'nameEn' | 'nameRu'>,
  language: string,
): string {
  const lang = language.toLowerCase().slice(0, 2);
  switch (lang) {
    case 'de':
      return account.nameDe || account.name;
    case 'en':
      return account.nameEn || account.name;
    case 'ru':
      return account.nameRu || account.name;
    default:
      return account.name;
  }
}
