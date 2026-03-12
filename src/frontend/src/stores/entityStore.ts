import { create } from 'zustand';

export interface LegalEntity {
  id: string;
  name: string;
  legalForm: string;
  taxId: string;
  currency: string;
}

interface EntityState {
  entities: LegalEntity[];
  selectedEntityId: string | null;
  setEntities: (entities: LegalEntity[]) => void;
  setSelectedEntity: (entityId: string) => void;
}

const STORAGE_KEY = 'cb_selected_entity_id';

function getPersistedEntityId(): string | null {
  return localStorage.getItem(STORAGE_KEY) ?? sessionStorage.getItem(STORAGE_KEY);
}

function persistEntityId(entityId: string): void {
  const rememberMe = localStorage.getItem('cb_remember_me') === 'true';
  (rememberMe ? localStorage : sessionStorage).setItem(STORAGE_KEY, entityId);
}

export function clearPersistedEntityId(): void {
  localStorage.removeItem(STORAGE_KEY);
  sessionStorage.removeItem(STORAGE_KEY);
}

export const useEntityStore = create<EntityState>((set) => ({
  entities: [],
  selectedEntityId: getPersistedEntityId(),
  setEntities: (entities) => set({ entities }),
  setSelectedEntity: (entityId) => {
    persistEntityId(entityId);
    set({ selectedEntityId: entityId });
  },
}));
