(() => {
  'use strict';

  const I18N = {
    ru: {
      checkingFfmpeg:'Проверка FFmpeg…', enginePathTitle:'Указать папку Steam / Wallpaper Engine', enginePathReset:'Вернуть автоматический поиск', refreshLibrary:'Обновить библиотеку',
      library:'Библиотека', searchWallpapers:'Поиск обоев…', wallpaperType:'Тип обоев', all:'Все', sorting:'Сортировка', librarySorting:'Сортировка библиотеки', sortName:'Имя A–Z', sortRecent:'Недавно обновлённые', sortType:'Тип', sortSource:'Источник', installedWallpapers:'Установленные обои', scanning:'Сканирование…',
      projectFolder:'Папка проекта', chooseWallpaperLibrary:'Выберите обои в библиотеке', chooseWallpaper:'Выберите обои', metadataWillAppear:'Метаданные и параметры экспорта появятся здесь.', nativeShort:'Исходное', resolution:'Разрешение', music:'Музыка',
      export:'Экспорт', format:'Формат', framesPerSecond:'кадров/с', duration:'Длительность', sec:'сек', recordingTime:'время записи', width:'Ширина', height:'Высота',
      audio:'Звук', wallpaperOriginalAudio:'Оригинальный звук обоев', recordAudio:'Записывать звук', backgroundCapture:'Фоновый захват', backgroundCaptureHint:'Окно Wallpaper Engine не мешает работе', renderInBackground:'Рендерить в фоне',
      cleanExportManual:'Clean Export · вручную', cleanIntro:'QFact.WE2Video покажет доступные корневые переключатели этих обоев. Выбери, что отключить перед записью.', selectAll:'Выбрать все', clearAll:'Снять всё', cleanChooseScene:'Выбери Scene/Web-обои, чтобы увидеть их переключатели.',
      folder:'Папка', change:'Изменить', open:'Открыть', exportVerb:'Экспортировать', lastExport:'Последний экспорт', done:'Готово', showFile:'Показать файл',
      pathSaved:'Путь сохранён', autoSearchEnabled:'Автопоиск включён', autoSearchRestored:'Steam / Wallpaper Engine снова определяется автоматически', manualPath:'Ручной путь: {0}', pathSet:'Путь задан', resetManualPath:'Сбросить ручной путь и вернуть автопоиск', ffmpegUnavailable:'FFmpeg недоступен', captureReady:'WGC / FFmpeg готов',
      shown:'Показано {0} из {1}', libraryEmptyManual:'Библиотека пуста · можно указать Steam / WE вручную', nothingFound:'Ничего не найдено', wallpapersNotFound:'Обои не найдены · проверь путь Steam / WE', localProject:'Локальный проект', untitled:'Без названия', authorUnknown:'Автор не указан', detecting:'Определяю…', metadata:'метаданные', videoDuration:'Видео: {0}', videoDurationNone:'Длительность видео: —', unavailable:'недоступен', nativeSuffix:'исходное',
      flagAudio:'звук', flagBackground:'фон', selectedCount:'{0} выбрано', currentlyOn:'сейчас включено', currentlyOff:'сейчас выключено', currentValue:'сейчас: {0}', notRequired:'не требуется', switchOne:'переключатель', switchFew:'переключателя', switchMany:'переключателей',
      cleanVideoHint:'Для Video исходный файл конвертируется напрямую — пользовательские свойства Wallpaper Engine не участвуют.', cleanVideoEmpty:'Clean Export не нужен для Video Wallpaper.', cleanNoProps:'В project.json не найдено bool/checkbox или безопасных on/off combo-переключателей.', cleanNoPropsEmpty:'У этих обоев нет доступных переключателей, которые QFact.WE2Video может безопасно выключить.', cleanManualHint:'Выбери корневые переключатели, которые нужно выключить только для этой записи. Подпункты модулей скрыты и обрабатываются каскадно.', module:'Модуль · {0} подп.', switchLabel:'Переключатель', hiddenSwitches:'+ {0} скрыт. switch',
      gifNoAudio:'GIF не поддерживает звук', videoAudioDirect:'Берётся напрямую из исходного видео', processLoopbackRequired:'Нужна Windows 10 2004+ для process loopback', ffmpegAudioMissing:'В FFmpeg нет нужного аудиоэнкодера', processAudioOnly:'Только звук Wallpaper Engine, не всего ПК', rendering:'Рендеринг…',
      exportStarted:'Экспорт запущен', inBackground:'в фоне', exportReady:'Экспорт готов', exportReadyWarning:'Экспорт готов с предупреждением', error:'Ошибка', unknownError:'Неизвестная ошибка', soundIncluded:'звук ✓', soundMissing:'без звука', soundOff:'звук выкл',
      helpTitle:'Помощь', close:'Закрыть', gotIt:'Понятно', helpHeading:'Как пользоваться', helpIntro:'Если коротко: выбери обои, настрой результат и нажми «Экспортировать». Остальное приложение сделает само.',
      helpQuickTitle:'Самый быстрый вариант', helpQuickText:'Нужен обычный ролик? Выбери обои → оставь MP4 · H.264 → выбери 1080p или 2K → при необходимости оставь звук → нажми «Экспортировать». Всё.',
      helpStep1Title:'Выбери обои', helpStep1Text:'Слева находится вся найденная библиотека Wallpaper Engine. Scene и Web рендерятся через Wallpaper Engine, а Video конвертируется напрямую из исходного видеофайла.',
      helpStep2Title:'Настрой экспорт', helpStep2Text:'MP4 · H.264 — лучший вариант по умолчанию. Выбери разрешение, FPS и длительность. «Исходное» использует размер, который удалось определить у обоев.',
      helpStep3Title:'Звук и фон', helpStep3Text:'«Звук» добавляет аудио обоев. «Фоновый захват» позволяет продолжать пользоваться компьютером во время Scene/Web-записи — окно рендера не должно мешать работе.',
      helpStep4Title:'Clean Export', helpStep4Text:'Для Scene/Web можно вручную выбрать переключатели, которые нужно выключить только на время записи: часы, медиаплеер, персонажа, эффекты и любые другие доступные автором настройки. У модулей внутренние подпункты скрыты и обрабатываются автоматически.',
      helpStep5Title:'Экспортируй', helpStep5Text:'Нажми «Экспортировать» и дождись уведомления. Готовый файл появится в выбранной папке. После завершения его можно сразу показать в Проводнике.',
      helpStep6Title:'Если Steam не найден', helpStep6Text:'Нажми кнопку «Steam / WE» сверху и укажи Steam, SteamLibrary, steamapps или саму папку wallpaper_engine. Программа запомнит путь.',
      helpFormatsTitle:'Какой формат выбрать?', helpFormatsText:'MP4 · H.264 — почти всегда. HEVC — если важен меньший размер. WebM · VP9 — для веба. GIF — короткая анимация без звука. MKV/MOV — когда этого требует дальнейшая работа.',
      helpCleanNoteTitle:'Важно про Clean Export', helpCleanNoteText:'QFact.WE2Video показывает только те переключатели, которые реально опубликованы автором обоев. Если нужного элемента в списке нет, приложение не может безопасно выключить его без изменения самого проекта.',
      helpSafetyTitle:'Оригиналы в безопасности', helpSafetyText:'Программа не переписывает Workshop-проекты. Экспорт и временные настройки выполняются отдельно от оригинальных файлов Wallpaper Engine.',
      helpTroubleTitle:'Если что-то не работает', helpTroubleText:'Сначала нажми обновление библиотеки. Если Wallpaper Engine не найден — укажи путь вручную. Если не работает конкретный формат или звук — запусти doctor.bat из Release Kit и проверь FFmpeg. Для звука Scene/Web нужна Windows 10 2004 или новее.',
      helpFooter:'QFact.WE2Video · Сделано так, чтобы один раз настроить и дальше просто экспортировать.', supportDeveloper:'Поддержать разработчика',
      version:'Версия {0}'
    },
    en: {
      checkingFfmpeg:'Checking FFmpeg…', enginePathTitle:'Choose Steam / Wallpaper Engine folder', enginePathReset:'Restore automatic detection', refreshLibrary:'Refresh library',
      library:'Library', searchWallpapers:'Search wallpapers…', wallpaperType:'Wallpaper type', all:'All', sorting:'Sorting', librarySorting:'Library sorting', sortName:'Name A–Z', sortRecent:'Recently updated', sortType:'Type', sortSource:'Source', installedWallpapers:'Installed wallpapers', scanning:'Scanning…',
      projectFolder:'Project folder', chooseWallpaperLibrary:'Choose a wallpaper from the library', chooseWallpaper:'Choose wallpaper', metadataWillAppear:'Metadata and export settings will appear here.', nativeShort:'Native', resolution:'Resolution', music:'Audio',
      export:'Export', format:'Format', framesPerSecond:'frames/s', duration:'Duration', sec:'sec', recordingTime:'recording time', width:'Width', height:'Height',
      audio:'Audio', wallpaperOriginalAudio:'Original wallpaper audio', recordAudio:'Record audio', backgroundCapture:'Background capture', backgroundCaptureHint:'Wallpaper Engine window stays out of your way', renderInBackground:'Render in background',
      cleanExportManual:'Clean Export · manual', cleanIntro:'QFact.WE2Video lists available root switches for this wallpaper. Choose what to turn off before capture.', selectAll:'Select all', clearAll:'Clear all', cleanChooseScene:'Choose a Scene/Web wallpaper to see its switches.',
      folder:'Folder', change:'Change', open:'Open', exportVerb:'Export', lastExport:'Last export', done:'Done', showFile:'Show file',
      pathSaved:'Path saved', autoSearchEnabled:'Auto-detection enabled', autoSearchRestored:'Steam / Wallpaper Engine is detected automatically again', manualPath:'Manual path: {0}', pathSet:'Path set', resetManualPath:'Clear manual path and restore auto-detection', ffmpegUnavailable:'FFmpeg unavailable', captureReady:'WGC / FFmpeg ready',
      shown:'Showing {0} of {1}', libraryEmptyManual:'Library is empty · you can set Steam / WE manually', nothingFound:'Nothing found', wallpapersNotFound:'No wallpapers found · check the Steam / WE path', localProject:'Local project', untitled:'Untitled', authorUnknown:'Author not specified', detecting:'Detecting…', metadata:'metadata', videoDuration:'Video: {0}', videoDurationNone:'Video duration: —', unavailable:'unavailable', nativeSuffix:'native',
      flagAudio:'audio', flagBackground:'background', selectedCount:'{0} selected', currentlyOn:'currently on', currentlyOff:'currently off', currentValue:'current: {0}', notRequired:'not required', switchOne:'switch', switchFew:'switches', switchMany:'switches',
      cleanVideoHint:'Video wallpapers are converted directly from the source file, so Wallpaper Engine user properties are not involved.', cleanVideoEmpty:'Clean Export is not required for Video wallpapers.', cleanNoProps:'No bool/checkbox or safe on/off combo properties were found in project.json.', cleanNoPropsEmpty:'This wallpaper has no switches QFact.WE2Video can safely turn off.', cleanManualHint:'Choose the root switches to disable only for this capture. Module children stay hidden and are handled as a cascade.', module:'Module · {0} children', switchLabel:'Switch', hiddenSwitches:'+ {0} hidden switch(es)',
      gifNoAudio:'GIF does not support audio', videoAudioDirect:'Taken directly from the source video', processLoopbackRequired:'Windows 10 2004+ is required for process loopback', ffmpegAudioMissing:'Required audio encoder is missing from FFmpeg', processAudioOnly:'Wallpaper Engine audio only, not the whole PC', rendering:'Rendering…',
      exportStarted:'Export started', inBackground:'in background', exportReady:'Export complete', exportReadyWarning:'Export complete with warning', error:'Error', unknownError:'Unknown error', soundIncluded:'audio ✓', soundMissing:'no audio', soundOff:'audio off',
      helpTitle:'Help', close:'Close', gotIt:'Got it', helpHeading:'How to use it', helpIntro:'Short version: choose a wallpaper, set the output you want and click Export. The app handles the rest.',
      helpQuickTitle:'The fastest way', helpQuickText:'Just need a normal video? Pick a wallpaper → keep MP4 · H.264 → choose 1080p or 2K → leave audio on if you want it → click Export. That is it.',
      helpStep1Title:'Choose a wallpaper', helpStep1Text:'Your detected Wallpaper Engine library is on the left. Scene and Web wallpapers are rendered through Wallpaper Engine; Video wallpapers are converted directly from their source video file.',
      helpStep2Title:'Set the output', helpStep2Text:'MP4 · H.264 is the best default for most people. Choose resolution, FPS and duration. Native uses the wallpaper size QFact.WE2Video was able to detect.',
      helpStep3Title:'Audio and background capture', helpStep3Text:'Audio adds the wallpaper sound. Background capture lets you keep using the PC while Scene/Web is being recorded — the render window should stay out of your way.',
      helpStep4Title:'Clean Export', helpStep4Text:'For Scene/Web wallpapers you can manually choose switches to turn off only for this capture: clock, media player, character parts, effects or any other settings exposed by the author. Module children stay hidden and are handled automatically.',
      helpStep5Title:'Export', helpStep5Text:'Click Export and wait for the notification. The finished file is written to the selected folder, and you can reveal it in File Explorer immediately after completion.',
      helpStep6Title:'If Steam was not found', helpStep6Text:'Use the Steam / WE button at the top and choose Steam, SteamLibrary, steamapps or the wallpaper_engine folder itself. The app remembers the path.',
      helpFormatsTitle:'Which format should I use?', helpFormatsText:'MP4 · H.264 for almost everything. HEVC when smaller files matter. WebM · VP9 for web use. GIF for short silent animation. MKV/MOV when your next tool specifically needs them.',
      helpCleanNoteTitle:'About Clean Export', helpCleanNoteText:'QFact.WE2Video can only show switches the wallpaper author actually exposed. If an element is not listed, the app cannot safely disable it without modifying the project itself.',
      helpSafetyTitle:'Your originals stay untouched', helpSafetyText:'The app does not rewrite Workshop projects. Export and temporary property changes are kept separate from the original Wallpaper Engine files.',
      helpTroubleTitle:'If something does not work', helpTroubleText:'First refresh the library. If Wallpaper Engine is missing, choose its path manually. If a format or audio path fails, run doctor.bat from the Release Kit and check FFmpeg. Scene/Web audio requires Windows 10 2004 or newer.',
      helpFooter:'QFact.WE2Video · Set it up once, then just export.', supportDeveloper:'Support developer',
      version:'Version {0}'
    }
  };

  const state = {
    library: [], selectedId: null, selection: null, metadata: null, formats: [], resolutions: [],
    filter: 'All', query: '', sort: 'name', busy: false, outputFolder: '', ffmpegReady: false,
    captureReady: false, processAudioSupported: true, engineRoot: '', engineRootManual: false,
    cleanPlan: { count: 0, items: [] }, cleanSelections: Object.create(null),
    language: 'ru', languageSelected: false, version: '1.0.3'
  };

  const $ = id => document.getElementById(id);
  const els = {};
  let toastTimer = 0;

  function t(key, ...args) {
    let value = (I18N[state.language] || I18N.en)[key] ?? I18N.en[key] ?? key;
    args.forEach((arg, i) => { value = value.replaceAll(`{${i}}`, String(arg)); });
    return value;
  }

  function applyLanguage() {
    document.documentElement.lang = state.language;
    document.title = 'QFact.WE2Video';
    document.querySelectorAll('[data-i18n]').forEach(el => { el.textContent = t(el.dataset.i18n); });
    document.querySelectorAll('[data-i18n-placeholder]').forEach(el => { el.placeholder = t(el.dataset.i18nPlaceholder); });
    document.querySelectorAll('[data-i18n-title]').forEach(el => { el.title = t(el.dataset.i18nTitle); });
    document.querySelectorAll('[data-i18n-aria]').forEach(el => { el.setAttribute('aria-label', t(el.dataset.i18nAria)); });
    if (els.languageButton) els.languageButton.textContent = state.language.toUpperCase();
    if (els.languageGate) els.languageGate.classList.toggle('hidden', !!state.languageSelected);
    renderEnginePath();
    renderLibrary();
    if (state.selection) { renderSelection(); renderMetadata(); renderCleanPlan(); updateCaptureOptions(); updateExportButton(); }
    renderFormats();
    renderResolutions();
  }

  document.addEventListener('DOMContentLoaded', () => {
    Object.assign(els, {
      engineStatus:$('engineStatus'), enginePathButton:$('enginePathButton'), enginePathLabel:$('enginePathLabel'), clearEnginePathButton:$('clearEnginePathButton'), languageButton:$('languageButton'), languageGate:$('languageGate'), helpButton:$('helpButton'), helpModal:$('helpModal'), helpCloseButton:$('helpCloseButton'), helpGotItButton:$('helpGotItButton'), supportDeveloperButton:$('supportDeveloperButton'), supportHeaderButton:$('supportHeaderButton'), refreshButton:$('refreshButton'), libraryCount:$('libraryCount'),
      searchInput:$('searchInput'), filterChips:$('filterChips'), sortSelect:$('sortSelect'), wallpaperList:$('wallpaperList'), libraryFooterText:$('libraryFooterText'),
      typeBadge:$('typeBadge'), sourceBadge:$('sourceBadge'), workshopBadge:$('workshopBadge'), openWallpaperFolder:$('openWallpaperFolder'),
      previewStage:$('previewStage'), previewBackdrop:$('previewBackdrop'), previewImage:$('previewImage'), previewEmpty:$('previewEmpty'),
      wallpaperTitle:$('wallpaperTitle'), wallpaperSubtitle:$('wallpaperSubtitle'), resolutionHero:$('resolutionHero'), metricResolution:$('metricResolution'), metricResolutionSource:$('metricResolutionSource'), metricMusic:$('metricMusic'), metricVideoDuration:$('metricVideoDuration'), metricWorkshop:$('metricWorkshop'), metricAuthor:$('metricAuthor'), tagRow:$('tagRow'), metadataNote:$('metadataNote'),
      formatSelect:$('formatSelect'), formatCodec:$('formatCodec'), formatDescription:$('formatDescription'), resolutionSelect:$('resolutionSelect'), resolutionHint:$('resolutionHint'), fpsInput:$('fpsInput'), durationInput:$('durationInput'), customResolution:$('customResolution'), widthInput:$('widthInput'), heightInput:$('heightInput'),
      audioToggle:$('audioToggle'), audioHint:$('audioHint'), backgroundCaptureToggle:$('backgroundCaptureToggle'), manualCleanPanel:$('manualCleanPanel'), cleanCount:$('cleanCount'), cleanSelectedCount:$('cleanSelectedCount'), cleanHint:$('cleanHint'), cleanItems:$('cleanItems'), cleanSelectAll:$('cleanSelectAll'), cleanClearAll:$('cleanClearAll'),
      outputFolderText:$('outputFolderText'), browseFolder:$('browseFolder'), openFolder:$('openFolder'), exportButton:$('exportButton'), exportButtonMeta:$('exportButtonMeta'), lastExport:$('lastExport'), lastExportText:$('lastExportText'), openLastExport:$('openLastExport'), toast:$('toast'), toastTitle:$('toastTitle'), toastMessage:$('toastMessage')
    });
    bindEvents();
    send('ready', {});
    if (new URLSearchParams(location.search).has('mock')) loadMockData();
  });

  function bindEvents() {
    els.refreshButton.addEventListener('click', () => send('refreshLibrary', {}));
    els.languageButton.addEventListener('click', () => send('setLanguage', { language: state.language === 'ru' ? 'en' : 'ru' }));
    els.languageGate.addEventListener('click', e => { const b=e.target.closest('[data-language]'); if(b) send('setLanguage',{language:b.dataset.language}); });
    els.helpButton.addEventListener('click', openHelp);
    els.helpCloseButton.addEventListener('click', closeHelp);
    els.helpGotItButton.addEventListener('click', closeHelp);
    const openSupport=()=>send('openExternal',{url:'https://dalink.to/daewri'});
    els.supportDeveloperButton?.addEventListener('click',openSupport);
    els.supportHeaderButton?.addEventListener('click',openSupport);
    els.helpModal.addEventListener('click', e => { if(e.target===els.helpModal) closeHelp(); });
    document.addEventListener('keydown', e => { if(e.key==='Escape' && !els.helpModal.classList.contains('hidden')) closeHelp(); });
    els.searchInput.addEventListener('input', () => { state.query = els.searchInput.value.trim().toLocaleLowerCase(); renderLibrary(); });
    els.sortSelect.addEventListener('change', () => { state.sort = els.sortSelect.value || 'name'; renderLibrary(); });
    els.filterChips.addEventListener('click', event => { const button=event.target.closest('[data-filter]'); if(!button) return; state.filter=button.dataset.filter; [...els.filterChips.querySelectorAll('.filter-chip')].forEach(x=>x.classList.toggle('active',x===button)); renderLibrary(); });
    els.wallpaperList.addEventListener('click', event => { const item=event.target.closest('.wallpaper-item'); if(!item||state.busy) return; selectWallpaper(item.dataset.id); });
    els.formatSelect.addEventListener('change', () => { updateFormatUi(); updateExportButton(); });
    els.resolutionSelect.addEventListener('change', () => { updateResolutionUi(); updateExportButton(); });
    [els.widthInput,els.heightInput,els.fpsInput,els.durationInput].forEach(x=>x.addEventListener('input',updateExportButton));
    [els.audioToggle,els.backgroundCaptureToggle].forEach(x=>x.addEventListener('change',updateExportButton));
    els.previewImage.addEventListener('load',()=>{ if(state.metadata&&Number(state.metadata.width)>0&&Number(state.metadata.height)>0)return; if(els.previewImage.naturalWidth>0&&els.previewImage.naturalHeight>0)setPreviewRatio(els.previewImage.naturalWidth,els.previewImage.naturalHeight); });
    els.enginePathButton.addEventListener('click',()=>send('browseEngineRoot',{})); els.clearEnginePathButton.addEventListener('click',()=>send('clearEngineRoot',{}));
    els.cleanItems.addEventListener('change',onCleanSelectionChanged); els.cleanSelectAll.addEventListener('click',()=>setAllCleanSelections(true)); els.cleanClearAll.addEventListener('click',()=>setAllCleanSelections(false));
    els.browseFolder.addEventListener('click',()=>send('browseFolder',{})); els.openFolder.addEventListener('click',()=>send('openFolder',{})); els.openWallpaperFolder.addEventListener('click',()=>send('openWallpaperFolder',{})); els.openLastExport.addEventListener('click',()=>send('openLastExport',{})); els.exportButton.addEventListener('click',exportCurrent);
    document.addEventListener('keydown', event => { if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='k'){event.preventDefault();els.searchInput.focus();els.searchInput.select();} if((event.ctrlKey||event.metaKey)&&event.key==='Enter'){event.preventDefault();if(!els.exportButton.disabled)exportCurrent();} });
    if(window.chrome?.webview) window.chrome.webview.addEventListener('message',event=>handleMessage(event.data));
  }

  function send(type,data){ if(window.chrome?.webview) window.chrome.webview.postMessage({type,data}); }

  function handleMessage(message){
    if(!message||typeof message!=='object')return; const {type,data}=message;
    switch(type){
      case 'config':
        state.outputFolder=data.outputFolder||''; state.resolutions=data.resolutions||[]; state.formats=data.formats||[]; state.engineRoot=data.engineRoot||''; state.engineRootManual=!!data.engineRootManual; state.language=data.language==='en'?'en':'ru'; state.languageSelected=!!data.languageSelected; state.version=data.version||state.version; renderConfig(); applyLanguage(); break;
      case 'library': state.library=data.items||[]; state.selectedId=data.selectedId||state.selectedId; renderLibrary(); break;
      case 'capabilities': state.ffmpegReady=!!data.ffmpegFound; state.captureReady=!!data.ffmpegFound&&data.capture!=='missing'; state.processAudioSupported=data.processAudioSupported!==false; state.formats=data.formats||state.formats; renderCapabilities(data); renderFormats(); updateCaptureOptions(); updateExportButton(); break;
      case 'selection': state.selectedId=data.id; state.selection=data; state.metadata=null; renderLibrary(); renderSelection(); state.cleanPlan={count:0,items:[]}; renderCleanPlan(); updateCaptureOptions(); updateExportButton(); break;
      case 'cleanPlan': state.cleanPlan=data||{count:0,items:[]}; renderCleanPlan(); updateCaptureOptions(); updateExportButton(); break;
      case 'metadata': state.metadata=data; renderMetadata(); updateResolutionUi(true); updateExportButton(); break;
      case 'outputFolder': state.outputFolder=data.path||''; els.outputFolderText.textContent=compactPath(state.outputFolder); els.outputFolderText.title=state.outputFolder; break;
      case 'engineRootChanged': state.engineRoot=data.path||''; state.engineRootManual=!!data.manual; renderEnginePath(); showToast(state.engineRootManual?t('pathSaved'):t('autoSearchEnabled'),state.engineRootManual?compactPath(state.engineRoot):t('autoSearchRestored')); break;
      case 'busy': setBusy(!!data.busy); break;
      case 'status': els.libraryFooterText.textContent=data.text||''; break;
      case 'exportStarted': showToast(t('exportStarted'),`${data.format} · ${data.width}×${data.height} · ${data.fps} FPS${data.backgroundCapture?` · ${t('inBackground')}`:''}${data.includeAudio?` · ${t('flagAudio')}`:''}`); break;
      case 'exportComplete': { els.lastExport.classList.remove('hidden'); const sound=data.audioRequested?(data.audioIncluded?t('soundIncluded'):t('soundMissing')):t('soundOff'); const clean=Number(data.cleanDetected||0)>0?` · Clean ${data.cleanSuccess||0}/${data.cleanDetected}`:''; els.lastExportText.textContent=`${data.format} · ${Number(data.sizeMb||0).toFixed(1)} MB · ${sound}${clean}`; const warning=data.audioWarning||Number(data.cleanFailed||0)>0; const msg=`${Number(data.sizeMb||0).toFixed(1)} MB · ${sound}${clean}${data.audioWarning?` · ${data.audioWarning}`:''}`; showToast(warning?t('exportReadyWarning'):t('exportReady'),msg,warning?'warning':'success'); break; }
      case 'error': showToast(t('error'),data.message||t('unknownError'),'error'); break;
    }
  }

  function renderConfig(){ renderResolutions(); renderFormats(); els.outputFolderText.textContent=compactPath(state.outputFolder); els.outputFolderText.title=state.outputFolder; renderEnginePath(); if(!els.resolutionSelect.value)els.resolutionSelect.value='1080p'; if(!els.formatSelect.value)els.formatSelect.value='mp4-h264'; updateResolutionUi(); updateFormatUi(); }

  function renderEnginePath(){ if(!els.enginePathButton)return; const manual=!!state.engineRootManual&&!!state.engineRoot; els.enginePathLabel.textContent=manual?t('pathSet'):'Steam / WE'; els.enginePathButton.classList.toggle('active',manual); els.enginePathButton.title=manual?t('manualPath',state.engineRoot):t('enginePathTitle'); els.clearEnginePathButton.classList.toggle('hidden',!manual); els.clearEnginePathButton.title=manual?t('resetManualPath'):''; }
  function renderCapabilities(data){ els.engineStatus.classList.remove('ready','error','pending'); els.engineStatus.classList.add(state.ffmpegReady?'ready':'error'); els.engineStatus.querySelector('span').textContent=data.engineLabel||(state.ffmpegReady?t('captureReady'):t('ffmpegUnavailable')); }

  function renderLibrary(){
    const query=state.query; const filtered=state.library.filter(item=>{const typeOk=state.filter==='All'||item.type===state.filter; const queryOk=!query||String(item.title||'').toLocaleLowerCase().includes(query)||(item.workshopId||'').includes(query); return typeOk&&queryOk;});
    const locale=state.language==='ru'?'ru':'en'; const compareText=(a,b)=>String(a||'').localeCompare(String(b||''),locale,{sensitivity:'base',numeric:true});
    filtered.sort((a,b)=>{switch(state.sort){case'recent':return Number(b.updatedAt||0)-Number(a.updatedAt||0)||compareText(a.title,b.title);case'type':return compareText(a.type,b.type)||compareText(a.title,b.title);case'source':return compareText(a.source,b.source)||compareText(a.title,b.title);default:return compareText(a.title,b.title);}});
    els.libraryCount.textContent=filtered.length===state.library.length?String(state.library.length):`${filtered.length}/${state.library.length}`;
    els.libraryFooterText.textContent=state.library.length?t('shown',filtered.length,state.library.length):t('libraryEmptyManual');
    if(!filtered.length){els.wallpaperList.innerHTML=`<div class="preview-empty" style="height:140px"><span>${escapeHtml(state.library.length?t('nothingFound'):t('wallpapersNotFound'))}</span></div>`;return;}
    els.wallpaperList.innerHTML=filtered.map(item=>{const selected=item.id===state.selectedId?' selected':''; const meta=item.workshopId?`#${item.workshopId}`:'LOCAL'; const title=item.workshopId?`Workshop ${item.workshopId}`:t('localProject'); return `<button class="wallpaper-item${selected}" type="button" role="option" aria-selected="${item.id===state.selectedId}" data-id="${escapeAttr(item.id)}"><span class="wallpaper-main"><strong>${escapeHtml(item.title)}</strong><span class="wallpaper-sub"><b>${escapeHtml(item.type)}</b><i>·</i><span>${escapeHtml(item.source)}</span></span></span><span class="wallpaper-meta-chip" title="${escapeAttr(title)}">${escapeHtml(meta)}</span></button>`;}).join('');
    requestAnimationFrame(()=>{const selected=els.wallpaperList.querySelector('.wallpaper-item.selected');if(selected&&!isElementVisible(selected,els.wallpaperList))selected.scrollIntoView({block:'nearest'});});
  }

  function selectWallpaper(id){if(!id||id===state.selectedId)return;state.selectedId=id;renderLibrary();send('selectWallpaper',{id});}
  function renderSelection(){const s=state.selection;if(!s)return;els.typeBadge.textContent=s.type||'Unknown';els.sourceBadge.textContent=s.source||'—';els.workshopBadge.classList.toggle('hidden',!s.workshopId);els.workshopBadge.textContent=s.workshopId?`Workshop ${s.workshopId}`:'Workshop';els.wallpaperTitle.textContent=s.title||t('untitled');els.wallpaperSubtitle.textContent=[s.type,s.source,s.author].filter(Boolean).join(' · ');els.metricWorkshop.textContent=s.workshopId?`#${s.workshopId}`:t('localProject');els.metricAuthor.textContent=s.author||t('authorUnknown');els.metricAuthor.title=s.folder||''; if(s.preview){els.previewImage.src=s.preview;els.previewImage.classList.remove('hidden');els.previewEmpty.classList.add('hidden');els.previewBackdrop.style.backgroundImage=`url("${s.preview}")`;}else{els.previewImage.removeAttribute('src');els.previewImage.classList.add('hidden');els.previewEmpty.classList.remove('hidden');els.previewBackdrop.style.backgroundImage='none';}setPreviewRatio(null,null);els.metricResolution.textContent=t('detecting');els.metricResolutionSource.textContent=t('metadata');els.metricMusic.textContent=t('detecting');els.metricVideoDuration.textContent='—';els.resolutionHero.querySelector('strong').textContent='…';els.tagRow.innerHTML='';els.metadataNote.classList.add('hidden');}
  function renderMetadata(){const m=state.metadata;if(!m)return;els.metricResolution.textContent=m.resolutionText||'—';els.metricResolutionSource.textContent=m.resolutionSource||'—';els.metricMusic.textContent=m.music||'—';els.metricVideoDuration.textContent=m.videoDuration?t('videoDuration',m.videoDuration):t('videoDurationNone');els.resolutionHero.querySelector('strong').textContent=m.resolutionText||'—';setPreviewRatio(Number(m.width),Number(m.height));const tags=Array.isArray(m.tags)?m.tags.slice(0,12):[];els.tagRow.innerHTML=tags.map(tag=>`<span class="meta-tag">${escapeHtml(tag)}</span>`).join('');if(m.note){els.metadataNote.textContent=m.note;els.metadataNote.classList.remove('hidden');}else els.metadataNote.classList.add('hidden');}

  function renderFormats(){const current=els.formatSelect?.value||'mp4-h264';if(!els.formatSelect)return;els.formatSelect.innerHTML=state.formats.map(format=>{const unsupported=format.supported===false;return `<option value="${escapeAttr(format.id)}" ${unsupported?'disabled':''}>${escapeHtml(format.label)}${unsupported?` · ${escapeHtml(t('unavailable'))}`:''}</option>`;}).join('');if([...els.formatSelect.options].some(x=>x.value===current&&!x.disabled))els.formatSelect.value=current;else if([...els.formatSelect.options].some(x=>x.value==='mp4-h264'))els.formatSelect.value='mp4-h264';updateFormatUi();}
  function renderResolutions(){if(!els.resolutionSelect)return;const current=els.resolutionSelect.value||'1080p';els.resolutionSelect.innerHTML=state.resolutions.map(p=>`<option value="${escapeAttr(p.id)}">${escapeHtml(p.label)}${p.width?` · ${p.width}×${p.height}`:''}</option>`).join('');els.resolutionSelect.value=state.resolutions.some(x=>x.id===current)?current:'1080p';}
  function setPreviewRatio(width,height){els.previewStage.style.setProperty('--preview-ratio','1.7777778');els.previewStage.classList.remove('portrait','ultrawide');}
  function updateFormatUi(){const format=state.formats.find(x=>x.id===els.formatSelect.value);if(!format)return;els.formatCodec.textContent=format.codec||'';els.formatDescription.textContent=`${format.label} · ${format.description||''}`;updateCaptureOptions();updateExportButton();}
  function updateResolutionUi(fromMetadata=false){const preset=state.resolutions.find(x=>x.id===els.resolutionSelect.value);const native=state.metadata&&Number(state.metadata.width)>0&&Number(state.metadata.height)>0;const nativeOption=[...els.resolutionSelect.options].find(x=>x.value==='native');if(nativeOption)nativeOption.disabled=!native;if(els.resolutionSelect.value==='native'&&native){els.widthInput.value=state.metadata.width;els.heightInput.value=state.metadata.height;els.resolutionHint.textContent=`${state.metadata.width} × ${state.metadata.height} · ${t('nativeSuffix')}`;els.customResolution.classList.add('hidden');}else if(els.resolutionSelect.value==='custom'){els.customResolution.classList.remove('hidden');els.resolutionHint.textContent=`${els.widthInput.value} × ${els.heightInput.value}`;}else if(preset&&preset.width){els.widthInput.value=preset.width;els.heightInput.value=preset.height;els.resolutionHint.textContent=`${preset.width} × ${preset.height}`;els.customResolution.classList.add('hidden');}else if(fromMetadata&&els.resolutionSelect.value==='native'&&!native){els.resolutionSelect.value='1080p';updateResolutionUi();return;}updateExportButton();}

  function updateExportButton(){const format=state.formats.find(x=>x.id===els.formatSelect.value);const selected=!!state.selection;const supported=!format||format.supported!==false;const isVideo=state.selection?.type==='Video';const captureOk=isVideo?state.ffmpegReady:state.captureReady;const audioRequested=!!els.audioToggle?.checked&&format?.supportsAudio!==false;const audioOk=!audioRequested||isVideo||(state.processAudioSupported&&format?.audioSupported!==false);els.exportButton.disabled=state.busy||!selected||!supported||!captureOk||!audioOk;const res=`${els.widthInput.value}×${els.heightInput.value}`;const formatText=format?.label||'MP4';const flags=[];if(audioRequested)flags.push(t('flagAudio'));if(!isVideo&&els.backgroundCaptureToggle.checked)flags.push(t('flagBackground'));const cleanSelected=getSelectedCleanKeys().length;if(!isVideo&&cleanSelected>0)flags.push(`Clean ${cleanSelected}`);els.exportButtonMeta.textContent=`${formatText} · ${res} · ${els.fpsInput.value} FPS${flags.length?` · ${flags.join(' · ')}`:''}`;}
  function exportCurrent(){if(els.exportButton.disabled||!state.selection)return;const width=clampInt(els.widthInput.value,64,16384,1920),height=clampInt(els.heightInput.value,64,16384,1080),fps=clampInt(els.fpsInput.value,1,240,60),duration=clampFloat(els.durationInput.value,.1,86400,10);send('export',{formatId:els.formatSelect.value,width,height,fps,duration,outputFolder:state.outputFolder,includeAudio:els.audioToggle.checked,backgroundCapture:els.backgroundCaptureToggle.checked,cleanKeys:getSelectedCleanKeys()});}

  function getSelectedCleanSet(){if(!state.selectedId)return new Set();const stored=state.cleanSelections[state.selectedId];return new Set(Array.isArray(stored)?stored:[]);} function setSelectedCleanSet(set){if(state.selectedId)state.cleanSelections[state.selectedId]=[...set];} function getSelectedCleanKeys(){const available=new Set((state.cleanPlan?.items||[]).map(x=>x.key));return[...getSelectedCleanSet()].filter(key=>available.has(key));}
  function onCleanSelectionChanged(event){const input=event.target.closest('input[data-clean-key]');if(!input||!state.selectedId)return;const set=getSelectedCleanSet();if(input.checked)set.add(input.dataset.cleanKey);else set.delete(input.dataset.cleanKey);setSelectedCleanSet(set);renderCleanSelectionCount();updateExportButton();}
  function setAllCleanSelections(enabled){if(!state.selectedId||state.selection?.type==='Video')return;state.cleanSelections[state.selectedId]=enabled?(state.cleanPlan?.items||[]).map(x=>x.key):[];renderCleanPlan();updateExportButton();}
  function renderCleanSelectionCount(){const selected=getSelectedCleanKeys().length;els.cleanSelectedCount.textContent=t('selectedCount',selected);els.cleanSelectedCount.classList.toggle('active',selected>0);}
  function formatCurrentState(item){if(item.currentEnabled===true)return t('currentlyOn');if(item.currentEnabled===false)return t('currentlyOff');if(item.currentValue!==undefined&&item.currentValue!==null)return t('currentValue',String(item.currentValue));return item.type||'switch';}
  function switchCount(count){if(state.language==='en')return `${count} ${count===1?t('switchOne'):t('switchMany')}`;const mod100=count%100,mod10=count%10;const key=mod100>=11&&mod100<=14?'switchMany':mod10===1?'switchOne':mod10>=2&&mod10<=4?'switchFew':'switchMany';return `${count} ${t(key)}`;}
  function renderCleanPlan(){const plan=state.cleanPlan||{count:0,items:[]},items=plan.items||[],count=Number(plan.count||items.length||0),isVideo=state.selection?.type==='Video';els.cleanCount.textContent=isVideo?t('notRequired'):switchCount(count);els.cleanCount.classList.toggle('active',count>0&&!isVideo);els.cleanSelectAll.disabled=isVideo||count===0;els.cleanClearAll.disabled=isVideo||count===0;if(isVideo){els.cleanHint.textContent=t('cleanVideoHint');els.cleanItems.innerHTML=`<div class="clean-empty">${escapeHtml(t('cleanVideoEmpty'))}</div>`;renderCleanSelectionCount();return;}if(!count){els.cleanHint.textContent=t('cleanNoProps');els.cleanItems.innerHTML=`<div class="clean-empty">${escapeHtml(t('cleanNoPropsEmpty'))}</div>`;renderCleanSelectionCount();return;}els.cleanHint.textContent=t('cleanManualHint');const selected=getSelectedCleanSet(),valid=new Set(items.map(x=>x.key));[...selected].forEach(key=>{if(!valid.has(key))selected.delete(key);});setSelectedCleanSet(selected);els.cleanItems.innerHTML=items.map(item=>{const checked=selected.has(item.key)?' checked':'';const moduleBadge=item.isModule?`<span class="module-badge">${escapeHtml(t('module',Number(item.childCount||0)))}</span>`:`<span class="module-badge switch-badge">${escapeHtml(t('switchLabel'))}</span>`;const cascade=item.isModule&&Number(item.hiddenSwitchCount||0)>0?`<small class="clean-cascade">${escapeHtml(t('hiddenSwitches',Number(item.hiddenSwitchCount)))}</small>`:'';return `<label class="manual-clean-item${item.currentEnabled===false?' currently-off':''}"><input type="checkbox" data-clean-key="${escapeAttr(item.key)}"${checked}><span class="manual-check" aria-hidden="true"></span><span class="manual-clean-copy"><strong>${escapeHtml(item.label||item.key)}</strong><small>${escapeHtml(formatCurrentState(item))} · ${escapeHtml(item.key)}</small></span><span class="manual-clean-meta">${moduleBadge}${cascade}</span></label>`;}).join('');renderCleanSelectionCount();}

  function updateCaptureOptions(){if(!els.audioToggle||!state.selection)return;const format=state.formats.find(x=>x.id===els.formatSelect.value),isVideo=state.selection.type==='Video',formatHasAudio=format?.supportsAudio!==false,processAudioOk=isVideo||state.processAudioSupported,codecAudioOk=!format||format.audioSupported!==false;els.audioToggle.disabled=!formatHasAudio||!processAudioOk||!codecAudioOk;if(els.audioToggle.disabled)els.audioToggle.checked=false;els.audioHint.textContent=!formatHasAudio?t('gifNoAudio'):isVideo?t('videoAudioDirect'):!state.processAudioSupported?t('processLoopbackRequired'):!codecAudioOk?t('ffmpegAudioMissing'):t('processAudioOnly');els.backgroundCaptureToggle.disabled=isVideo;if(isVideo)els.backgroundCaptureToggle.checked=true;renderCleanPlan();}
  function setBusy(busy){state.busy=busy;document.body.classList.toggle('busy',busy);els.exportButton.classList.toggle('busy',busy);if(busy){els.exportButton.disabled=true;els.exportButton.querySelector('strong').textContent=t('rendering');}else{els.exportButton.querySelector('strong').textContent=t('exportVerb');updateExportButton();}}
  function showToast(title,message,kind='success'){clearTimeout(toastTimer);els.toastTitle.textContent=title;els.toastMessage.textContent=message||'';els.toast.classList.toggle('error',kind==='error'||kind===true);els.toast.classList.toggle('warning',kind==='warning');els.toast.classList.add('show');toastTimer=setTimeout(()=>els.toast.classList.remove('show'),kind==='error'?6500:5000);}

  function openHelp(){els.helpModal.classList.remove('hidden');document.body.classList.add('modal-open');els.helpCloseButton.focus();}
  function closeHelp(){els.helpModal.classList.add('hidden');document.body.classList.remove('modal-open');els.helpButton.focus();}

  function loadMockData(){const sampleFormats=[{id:'mp4-h264',label:'MP4 • H.264',codec:'H.264 / AVC',description:'Maximum compatibility.',extension:'.mp4',supported:true,supportsAudio:true,audioSupported:true},{id:'gif',label:'GIF • Animated',codec:'GIF',description:'Animated GIF.',extension:'.gif',supported:true,supportsAudio:false,audioSupported:false}];const sampleRes=[{id:'720p',label:'HD 720p',width:1280,height:720},{id:'1080p',label:'Full HD 1080p',width:1920,height:1080},{id:'1440p',label:'QHD / 2K',width:2560,height:1440},{id:'2160p',label:'4K UHD',width:3840,height:2160},{id:'native',label:'Native',width:0,height:0},{id:'custom',label:'Custom',width:0,height:0}];handleMessage({type:'config',data:{outputFolder:'C:\\Users\\User\\Videos\\QFact.WE2Video',resolutions:sampleRes,formats:sampleFormats,language:'en',languageSelected:true,version:'1.0.3'}});handleMessage({type:'capabilities',data:{ffmpegFound:true,capture:'wgc',engineLabel:'WGC / FFmpeg ready',processAudioSupported:true,formats:sampleFormats}});const library=[['a','Guardian — Sci-Fi Monoliths (Dual Monitor)','Scene','Workshop','2080115941'],['b','[4K] Vagabond — Sorrow','Scene','Workshop','3570415532'],['c','Arknights: Endfield Wuling Grotto','Video','Workshop','3412510234']].map((x,i)=>({id:x[0],title:x[1],type:x[2],source:x[3],workshopId:x[4],updatedAt:Date.now()-i*86400000}));handleMessage({type:'library',data:{count:library.length,selectedId:'a',items:library}});handleMessage({type:'selection',data:{id:'a',title:'Guardian — Sci-Fi Monoliths (Dual Monitor)',type:'Scene',source:'Workshop',workshopId:'2080115941',author:'Space Architect',folder:'C:\\Steam\\...',preview:null,metadataLoading:true}});handleMessage({type:'metadata',data:{available:true,resolutionText:'5120×1440',resolutionSource:'Workshop tag',width:5120,height:1440,music:'3:42',videoDuration:null,tags:['Wallpaper','Scene','Sci-Fi'],note:''}});}

  function compactPath(path){if(!path)return'exports';if(path.length<=56)return path;return`${path.slice(0,22)}…${path.slice(-30)}`;}
  function clampInt(value,min,max,fallback){const n=Number.parseInt(value,10);return Number.isFinite(n)?Math.min(max,Math.max(min,n)):fallback;}
  function clampFloat(value,min,max,fallback){const n=Number.parseFloat(value);return Number.isFinite(n)?Math.min(max,Math.max(min,n)):fallback;}
  function escapeHtml(value){return String(value??'').replace(/[&<>'"]/g,ch=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[ch]));}
  function escapeAttr(value){return escapeHtml(value);} function isElementVisible(el,container){const a=el.getBoundingClientRect(),b=container.getBoundingClientRect();return a.top>=b.top&&a.bottom<=b.bottom;}
})();
