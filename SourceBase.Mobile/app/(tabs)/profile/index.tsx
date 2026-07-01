import React, { useState } from "react";
import {
  View,
  Text,
  TextInput,
  Pressable,
  Image,
  Alert,
  ScrollView,
  StyleSheet,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import * as ImagePicker from "expo-image-picker";
import { useAuth } from "../../../src/auth/AuthContext";
import {
  useProfile,
  useUpdateProfile,
  useUploadAvatar,
} from "../../../src/hooks/useProfile";
import { useTodoLists } from "../../../src/hooks/useTodos";
import { Chip } from "../../../src/components/Chip";
import { colors, radius, spacing } from "../../../src/theme/colors";

export default function Profile() {
  const { signOut } = useAuth();
  const { data: me } = useProfile();
  const update = useUpdateProfile();
  const uploadAvatar = useUploadAvatar();
  const lists = useTodoLists();

  const [firstName, setFirstName] = useState<string | undefined>();
  const [lastName, setLastName] = useState<string | undefined>();
  const [phone, setPhone] = useState<string | undefined>();

  const pickAvatar = async () => {
    const perm = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (!perm.granted) {
      Alert.alert("Permission needed", "Allow photo access to change your avatar.");
      return;
    }
    const res = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      quality: 0.7,
    });
    if (res.canceled) return;
    const asset = res.assets[0];
    const name = asset.fileName ?? `avatar-${Date.now()}.jpg`;
    try {
      await uploadAvatar.mutateAsync({ uri: asset.uri, name });
    } catch {
      Alert.alert("Upload failed", "Could not update your photo.");
    }
  };

  const save = () =>
    update.mutate({
      firstName: firstName ?? me?.firstName,
      lastName: lastName ?? me?.lastName,
      phoneNumber: phone ?? me?.phoneNumber,
    });

  return (
    <SafeAreaView edges={["top"]} style={{ flex: 1 }}>
    <ScrollView contentContainerStyle={styles.container}>
      <View style={styles.avatarBlock}>
        <Pressable onPress={pickAvatar}>
          {me?.avatarUrl ? (
            <Image source={{ uri: me.avatarUrl }} style={styles.avatar} />
          ) : (
            <View style={styles.avatarFallback}>
              <Text style={{ fontSize: 32 }}>
                {me?.firstName?.[0] ?? me?.email?.[0] ?? "?"}
              </Text>
            </View>
          )}
        </Pressable>
        <Text style={styles.name}>
          {me?.firstName} {me?.lastName}
        </Text>
        <Text style={styles.email}>{me?.email}</Text>
      </View>

      <Field label="First name" value={firstName ?? me?.firstName ?? ""} onChange={setFirstName} />
      <Field label="Last name" value={lastName ?? me?.lastName ?? ""} onChange={setLastName} />
      <Field label="Phone" value={phone ?? me?.phoneNumber ?? ""} onChange={setPhone} />

      <Text style={styles.label}>Default todo list</Text>
      <View style={styles.chips}>
        {lists.data?.items.map((l) => (
          <Chip
            key={l.id}
            label={l.name}
            active={me?.defaultTodoListId === l.id}
            onPress={() => update.mutate({ defaultTodoListId: l.id })}
          />
        ))}
      </View>

      <Pressable onPress={save} disabled={update.isPending} style={styles.saveBtn}>
        <Text style={styles.saveText}>Save changes</Text>
      </Pressable>

      <Pressable onPress={signOut} style={styles.logout}>
        <Text style={{ color: colors.expense, fontWeight: "600" }}>Log out</Text>
      </Pressable>
    </ScrollView>
    </SafeAreaView>
  );
}

function Field({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (s: string) => void;
}) {
  return (
    <View style={{ gap: spacing.xs }}>
      <Text style={styles.label}>{label}</Text>
      <TextInput value={value} onChangeText={onChange} style={styles.input} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { padding: spacing.xl, gap: spacing.lg },
  avatarBlock: { alignItems: "center", gap: spacing.sm },
  avatar: { width: 96, height: 96, borderRadius: 48 },
  avatarFallback: {
    width: 96,
    height: 96,
    borderRadius: 48,
    backgroundColor: colors.primaryTint,
    alignItems: "center",
    justifyContent: "center",
  },
  name: { fontWeight: "700", fontSize: 18 },
  email: { color: colors.textMuted },
  label: { color: colors.textMuted, fontSize: 12 },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  chips: { flexDirection: "row", flexWrap: "wrap", gap: spacing.sm },
  saveBtn: {
    backgroundColor: colors.primary,
    padding: spacing.lg,
    borderRadius: radius.lg,
    alignItems: "center",
  },
  saveText: { color: colors.white, fontWeight: "700" },
  logout: { padding: spacing.md, alignItems: "center" },
});
