package com.testing.tfg.config;

import org.springframework.stereotype.Component;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Component
public class GameConfig {

    private final Map<String, Object> settings = new ConcurrentHashMap<>();

    public GameConfig() {
        // Valores por defecto
        settings.put("sensitivity", "medium");
        settings.put("force", 45);
        settings.put("darkMode", false);
        settings.put("fontSize", 16);
    }

    public void setSensitivity(String sensitivity) {
        settings.put("sensitivity", sensitivity);
    }

    public String getSensitivity() {
        return (String) settings.getOrDefault("sensitivity", "medium");
    }

    public void setForce(int force) {
        settings.put("force", force);
    }

    public int getForce() {
        return (int) settings.getOrDefault("force", 45);
    }

    public void setDarkMode(boolean darkMode) {
        settings.put("darkMode", darkMode);
    }

    public boolean isDarkMode() {
        return (boolean) settings.getOrDefault("darkMode", false);
    }

    public void setFontSize(int fontSize) {
        settings.put("fontSize", fontSize);
    }

    public int getFontSize() {
        return (int) settings.getOrDefault("fontSize", 16);
    }

    public void updateFromMap(Map<String, Object> map) {
        if (map.containsKey("sensitivity")) {
            setSensitivity((String) map.get("sensitivity"));
        }
        if (map.containsKey("force")) {
            setForce(((Number) map.get("force")).intValue());
        }
        if (map.containsKey("darkMode")) {
            setDarkMode((Boolean) map.get("darkMode"));
        }
        if (map.containsKey("fontSize")) {
            setFontSize(((Number) map.get("fontSize")).intValue());
        }
    }

    public Map<String, Object> toMap() {
        return new ConcurrentHashMap<>(settings);
    }
}