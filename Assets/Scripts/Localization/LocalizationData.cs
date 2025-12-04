using System.Collections.Generic;

public static class LocalizationData
{
    public static readonly Dictionary<string, string> RU = new()
    {
        // === UI ===
        ["UI_SUBMIT"] = "Отправить отчёт",
        ["UI_CANCEL"] = "Отмена",
        ["UI_CLOSE"] = "Закрыть",
        ["UI_RECORD"] = "Записать данные",
        ["UI_AddScanText"] = "Поставьте образец на стенд чтобы начать сканирование",
        ["UI_MainMenu_NewGame"] = "Новая игра",
        ["UI_MainMenu_ContinueGame"] = "Продолжить игру",
        ["UI_SetingButton"] = "Настройки",
        ["UI_MainMenu_ExitGame"] = "Выйти из игры",

        // === Радио / Монологи ===
        ["RADIO_PROMPT"] = "Нажмите Enter, чтобы продолжить",
        ["RADIO_SPEAKER_COMMAND"] = "Радио",

        // LocalizationData.RU
        ["SCANNER_DEFAULT"] = "Крист. решётка: {0}\nВозраст: {1}\nРадиоактивность: {2}",
        ["CRYSTAL_LABEL"] = "{0}",
        ["AGE_LABEL"] = "{0} {1}",
        ["RAD_LABEL"] = "{0} Бк",

        ["SCANNER_NO_CONNECTION"] = "НЕТ СОЕДИНЕНИЯ",
        ["SCANNER_ALREADY_RESEARCHED"] = "ОБРАЗЕЦ УЖЕ ИЗУЧЕН\nОтчёт по нему отправлен.",
        ["SCANNER_ALREADY_DONE"] = "<color=#888888>Исследование завершено ранее</color>",
        ["SCANNER_NO_SIGNAL"] = "<color=red>Нет сигнала или исследование завершено!</color>\nКрист. решётка: ???\nВозраст: ???\nРадиоактивность: ???",

        ["REPORT_SELECT_CLASS"] = "Выберите класс для просмотра характеристик",
        ["REPORT_CHOOSE_CORRECT"] = "Выберите правильный класс",
        ["REPORT_SELECTED"] = "Выбран: {0}\nНажмите «Отправить отчёт»",
        ["REPORT_MEASURED_DATA"] = "<b>ИЗМЕРЕННЫЕ ДАННЫЕ:</b>\n\nВозраст: <color=#FFD700>{0}</color> {1}\nРадиация: <color=#FF6666>{2}</color> Бк\nРешётка: <color=#CC66FF>{3}</color>",
        ["REPORT_UNKNOWN"] = "Возраст: ???\nРадиация: ??? Бк\nРешётка: ???",
        ["REPORT_AGE"] = "Возраст: {0} {1}",
        ["REPORT_RAD"] = "Радиация: {0} Бк",
        ["REPORT_CRYSTAL"] = "Решётка: {0}",
        ["REPORT_DAYS"] = "дней",
        ["REPORT_MILLION_YEARS"] = "млн лет",
    ["REPORT_SELECT_FIRST"] = "Сначала выберите класс!",
        // === Метки данных ===
        

        // === Классы минералов ===
        ["CLASS_ANOMALY"] = "Аномалия",
        ["CLASS_NATIVE"] = "Самородные",
        ["CLASS_OXIDE"] = "Оксиды",
        ["CLASS_SULFIDE"] = "Сульфиды",
        ["CLASS_CARBONATE"] = "Карбонаты",

        // === Туториал ===
        ["TUT_LOOK"] = "Осмотритесь вокруг — двигайте мышью",
        ["TUT_MOVE"] = "Двигайтесь с помощью <color=#ffff00>W A S D</color>",
        ["TUT_DOOR"] = "Чтобы открыть дверь — подойдите и <color=#ffff00>зажмите ЛКМ</color>",
        ["TUT_VEHICLE"] = "Подойдите к снегоходу и нажмите <color=#ffff00>E</color>",
        ["TUT_FLARE"] = "Бросьте факел — нажмите <color=#ffff00>F</color>",
        ["TUT_BREAK"] = "Ломайте ледяную залежь — <color=#ffff00>ЛКМ</color>",
        ["TUT_CARRY"] = "Отнесите добытый образец\nв <color=#ffff00>снегоход</color>",
        ["TUT_RETURN"] = "Вы собрали достаточно образцов.\n<color=#00ff00>Вернитесь на базу и изучите их</color>",
        ["TUT_TABLE"] = "Возьмите образец и отнесите его\nна <color=#ffff00>исследовательский стол</color>",
        ["TUT_SCAN_MOVE"] = "Крутите джойстик на <color=#ffff00>LMB</color>, найдите активные зоны на образце!",
        ["TUT_SCAN_CLICK"] = "Нажмите на кнопку <color=#ffff00>получить данные</color>",
        ["TUT_ACCURACY"] = "Чем точнее вы выбрали точку на минерале,\nтем точнее вы получите данные.",
        ["TUT_FIND_MORE"] = "Найдите еще <color=#ffff00>2 точки</color>, чтобы собрать все данные",
        ["TUT_CONCLUSION"] = "Сделайте вывод, к какому классу относится образец — <color=#ffff00>отправьте отчёт</color>",
        ["TUT_ANOMALY_PLACE"] = "Поместите странный образец\nв <color=#ff3333>ящик для аномалий</color>",
        ["TUT_GO_TO_BED"] = "Лягте в кровать — нажмите <color=#ffff00>E</color>",

        // === Монологи (обновлённые под новую версию игры) ===
        ["MONOLOGUE_INTRO_01"] = "Приём... На связи руководство! Мы засекли масштабный звездопад в твоём секторе этой ночью.",
        ["MONOLOGUE_INTRO_02"] = "Твоя задача — съездить на предполагаемые координаты и изучить образцы минералов оттуда.",
        ["MONOLOGUE_INTRO_03"] = "Обломки скоро поглотят снега Антарктиды, а результаты исследований очень важны для экспедиции!",

        ["MONOLOGUE_RETURN_01"] = "Приём... Мы видим по камерам, что тебе удалось собрать минералы - отлично!",
        ["MONOLOGUE_RETURN_02"] = "Теперь тебе нужно проанализировать их - используй оборудование на базе.",
        ["MONOLOGUE_RETURN_03"] = "Как только определишь все характеристики образца - делай вывод что за минерал и присылай нам отчёт.",

        ["MONOLOGUE_FINAL_01"] = "Приём... На связи руководство... мы тоже наблюдаем странные характеристики образца...",
        ["MONOLOGUE_FINAL_02"] = "Вероятно, ошибка сканера! Не заморачивайся — такое бывает.",
        ["MONOLOGUE_FINAL_03"] = "Просто положи этот образец в карантинный ящик и поставь в отчёте раздел «Аномалия».",

        // === Названия классов минералов (для отчётов) ===
        ["NAME_MINERAL_CLASS_1"] = "Осадочный железняк",
        ["NAME_MINERAL_CLASS_2"] = "Радиоактивные породы",
        ["NAME_MINERAL_CLASS_3"] = "Окаменелая флора",
        ["NAME_MINERAL_CLASS_4"] = "Останки динозавров",
        ["NAME_MINERAL_CLASS_5"] = "Аномалия",

        // === Кристаллические решётки ===
        ["CRYSTAL_CUBIC"] = "кубическая",
        ["CRYSTAL_MOLECULAR"] = "молекулярная",
        ["CRYSTAL_MONOCLINIC"] = "моноклинная",
        ["CRYSTAL_AMORPHOUS"] = "аморфная",

        // В LocalizationData.RU
        ["REPORT_MEASURED_DATA"] = "<b>ИЗМЕРЕННЫЕ ДАННЫЕ:</b>\n\nВозраст: <color=#FFD700>{0}</color> {1}\nРадиация: <color=#FF6666>{2}</color> Бк\nРешётка: <color=#CC66FF>{3}</color>",
        ["REPORT_CLASS_DETAILS"] = "{0}\n\nВозраст: {1} {2}\nРадиоактивность: {3} Бк\nКрист. решётка: {4}",
        ["REPORT_CLASS_DETAILS_ANOMALY"] = "{0}\n\nВозраст: ???\nРадиоактивность: ???\nКрист. решётка: ???",
        ["REPORT_AGE_UNIT_DAYS"] = "дней",
        ["REPORT_AGE_UNIT_MILLION"] = "млн лет",
    };

    public static readonly Dictionary<string, string> EN = new()
    {
        // === UI ===
        ["UI_SUBMIT"] = "Submit Report",
        ["UI_CANCEL"] = "Cancel",
        ["UI_CLOSE"] = "Close",
        ["UI_RECORD"] = "Record Data",
        ["UI_AddScanText"] = "Place the sample on the stand to start scanning",
        ["UI_MainMenu_NewGame"] = "New Game",
        ["UI_MainMenu_ContinueGame"] = "Continue Game",
        ["UI_SetingButton"] = "Settings",
        ["UI_MainMenu_ExitGame"] = "Exit Game",

        // === Radio ===
        ["RADIO_PROMPT"] = "Press Enter to continue",
        ["RADIO_SPEAKER_COMMAND"] = "Radio",

        // === Scanner ===
        ["SCANNER_DEFAULT"] = "Crystal system: {0}\nAge: {1}\nRadioactivity: {2}",
        ["SCANNER_NO_CONNECTION"] = "NO CONNECTION",
        ["SCANNER_ALREADY_RESEARCHED"] = "SAMPLE ALREADY RESEARCHED\nReport has been sent.",
        ["SCANNER_ALREADY_DONE"] = "<color=#888888>Research completed previously</color>",
        ["SCANNER_NO_SIGNAL"] = "<color=red>No signal or research completed!</color>\nCrystal system: ???\nAge: ???\nRadioactivity: ???",

        ["REPORT_MEASURED_DATA"] = "<b>MEASURED DATA:</b>\n\nAge: <color=#FFD700>{0}</color> {1}\nRadiation: <color=#FF6666>{2}</color> Bq\nCrystal system: <color=#CC66FF>{3}</color>",
        ["REPORT_CLASS_DETAILS"] = "{0}\n\nAge: {1} {2}\nRadiation: {3} Bq\nCrystal system: {4}",
        ["REPORT_CLASS_DETAILS_ANOMALY"] = "{0}\n\nAge: ???\nRadiation: ??? Bq\nCrystal system: ???",
        ["REPORT_AGE_UNIT_DAYS"] = "days",
        ["REPORT_AGE_UNIT_MILLION"] = "million years",

        // === Data labels ===
        ["AGE_LABEL"] = "{0} {1}",
        ["RAD_LABEL"] = "{0} Bq",
        ["CRYSTAL_LABEL"] = "{0}",

        // === Mineral classes ===
        ["CLASS_ANOMALY"] = "Anomaly",
        ["CLASS_IRON"] = "Native Elements",
        ["CLASS_RADIOACTIVE"] = "Oxides",
        ["CLASS_FLORA"] = "Sulfides",
        ["CLASS_DINOSAUR"] = "Carbonates",
        ["REPORT_SELECT_CLASS"] = "Select a class to view its characteristics",
        ["REPORT_CHOOSE_CORRECT"] = "Choose the correct class",
        ["REPORT_SELECTED"] = "Selected: {0}\nPress «Submit Report»",
        ["REPORT_MEASURED_DATA"] = "<b>MEASURED DATA:</b>\n\nAge: <color=#FFD700>{0}</color> {1}\nRadiation: <color=#FF6666>{2}</color> Bq\nCrystal system: <color=#CC66FF>{3}</color>",
        ["REPORT_UNKNOWN"] = "Age: ???\nRadiation: ??? Bq\nCrystal system: ???",
        ["REPORT_AGE"] = "Age: {0} {1}",
        ["REPORT_RAD"] = "Radiation: {0} Bq",
        ["REPORT_CRYSTAL"] = "Crystal system: {0}",
        ["REPORT_DAYS"] = "days",
        ["REPORT_MILLION_YEARS"] = "million years",
        ["REPORT_SELECT_FIRST"] = "First select a class!",
        // === Tutorial ===
        ["TUT_LOOK"] = "Look around — move your mouse",
        ["TUT_MOVE"] = "Move using <color=#ffff00>W A S D</color>",
        ["TUT_DOOR"] = "To open the door — approach and <color=#ffff00>hold LMB</color>",
        ["TUT_VEHICLE"] = "Approach the snowmobile and press <color=#ffff00>E</color>",
        ["TUT_FLARE"] = "Throw a flare — press <color=#ffff00>F</color>",
        ["TUT_BREAK"] = "Break the ice deposit — <color=#ffff00>LMB</color>",
        ["TUT_CARRY"] = "Carry the sample\ninto the <color=#ffff00>snowmobile</color>",
        ["TUT_RETURN"] = "You've collected enough samples.\n<color=#00ff00>Return to base and analyze them</color>",
        ["TUT_TABLE"] = "Take the sample and bring it\nto the <color=#ffff00>research table</color>",
        ["TUT_SCAN_MOVE"] = "Move the joystick with <color=#ffff00>LMB</color>, find active zones on the sample!",
        ["TUT_SCAN_CLICK"] = "Press the <color=#ffff00>Record Data</color> button",
        ["TUT_ACCURACY"] = "The more accurately you aim,\nthe more precise data you'll get.",
        ["TUT_FIND_MORE"] = "Find <color=#ffff00>2 more points</color> to collect all data",
        ["TUT_CONCLUSION"] = "Conclude which class the sample belongs to — <color=#ffff00>submit report</color>",
        ["TUT_ANOMALY_PLACE"] = "Place the strange sample\nin the <color=#ff3333>anomaly box</color>",
        ["TUT_GO_TO_BED"] = "Go to bed — press <color=#ffff00>E</color>",

        // === Monologues (updated for current story) ===
        ["MONOLOGUE_INTRO_01"] = "Receiving... This is Command. We detected a massive meteor shower in your sector tonight.",
        ["MONOLOGUE_INTRO_02"] = "Your task is to go to the estimated coordinates and examine mineral samples from the impact site.",
        ["MONOLOGUE_INTRO_03"] = "The debris will soon be buried under Antarctic snow — these research results are critical for the expedition!",

        ["MONOLOGUE_RETURN_01"] = "Receiving... We can see on the cameras that you managed to collect the samples — excellent work!",
        ["MONOLOGUE_RETURN_02"] = "Now you need to analyze them — use the equipment at the base.",
        ["MONOLOGUE_RETURN_03"] = "Once you determine all the characteristics of the sample, classify the mineral and send us the report.",

        ["MONOLOGUE_FINAL_01"] = "Receiving... This is Command... we're seeing strange readings from that sample too...",
        ["MONOLOGUE_FINAL_02"] = "Most likely a scanner error. Don't worry — it happens sometimes.",
        ["MONOLOGUE_FINAL_03"] = "Just put this sample in the quarantine box and mark it as \"Anomaly\" in the report.",

        // === Mineral class names (for reports) ===
        ["NAME_MINERAL_CLASS_1"] = "Sedimentary Ironstone",
        ["NAME_MINERAL_CLASS_2"] = "Radioactive Rocks",
        ["NAME_MINERAL_CLASS_3"] = "Petrified Flora",
        ["NAME_MINERAL_CLASS_4"] = "Dinosaur Remains",
        ["NAME_MINERAL_CLASS_5"] = "Anomaly",

        // === Crystal systems ===
        ["CRYSTAL_CUBIC"] = "cubic",
        ["CRYSTAL_MOLECULAR"] = "molecular",
        ["CRYSTAL_MONOCLINIC"] = "monoclinic",
        ["CRYSTAL_AMORPHOUS"] = "amorphous",
    };
}