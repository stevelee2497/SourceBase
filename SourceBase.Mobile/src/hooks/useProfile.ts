import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { profileApi } from "../api/profile";
import { filesApi } from "../api/files";
import { qk } from "../utils/queryKeys";

export const useProfile = () =>
  useQuery({ queryKey: qk.me, queryFn: profileApi.get });

export function useUpdateProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: profileApi.update,
    onSuccess: () => qc.invalidateQueries({ queryKey: qk.me }),
  });
}

export function useUploadAvatar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (file: { uri: string; name: string }) => {
      const { uploadUrl, avatarUrl, contentType } = await filesApi.avatarUploadUrl(
        file.name
      );
      const blob = await (await fetch(file.uri)).blob();
      await filesApi.putToSignedUrl(uploadUrl, blob, contentType);
      await profileApi.update({ avatarUrl });
      return avatarUrl;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: qk.me }),
  });
}
