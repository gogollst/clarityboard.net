import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';

interface VersionInfo {
  version: string;
  buildDate: string;
}

export function useVersion() {
  return useQuery({
    queryKey: queryKeys.version.all(),
    queryFn: async () => {
      const { data } = await api.get<VersionInfo>('/version');
      return data;
    },
    staleTime: 10 * 60 * 1000, // 10 minutes
    retry: false,
  });
}
