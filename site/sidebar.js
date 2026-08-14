// Inject section nav tabs into top bar
(function() {
  var topBar = document.querySelector('.top-bar');
  if (!topBar) return;

  var path = window.location.pathname.replace(/\\/g, '/');
  var inMechanics = path.indexOf('/mechanics/') !== -1;
  var inCrashes = path.indexOf('/crashes/') !== -1;
  var prefix = (inMechanics || inCrashes) ? '../' : '';

  var moddingHref   = prefix + 'index.html';
  var mechanicsHref = prefix + 'mechanics/index.html';
  var crashesHref   = prefix + 'crashes/index.html';

  var nav = document.createElement('nav');
  nav.className = 'section-nav';
  nav.innerHTML =
    '<a href="' + moddingHref   + '" class="section-tab' + (!inMechanics && !inCrashes ? ' active' : '') + '">Modding Reference</a>' +
    '<a href="' + mechanicsHref + '" class="section-tab' + (inMechanics ? ' active' : '') + '">Game Mechanics</a>' +
    '<a href="' + crashesHref   + '" class="section-tab' + (inCrashes ? ' active' : '') + '">Crash Investigations</a>';

  var title = topBar.querySelector('.site-title');
  if (title) {
    title.after(nav);
  } else {
    topBar.prepend(nav);
  }
})();

// Collapsible sidebar sections
document.addEventListener('DOMContentLoaded', function() {
  var toggles = document.querySelectorAll('.sidebar h3.sidebar-toggle');

  toggles.forEach(function(toggle) {
    var links = toggle.nextElementSibling;
    if (!links || !links.classList.contains('sidebar-links')) return;

    // Auto-expand section containing the active link
    var hasActive = links.querySelector('a.active');
    if (hasActive) {
      toggle.classList.add('expanded');
      links.classList.add('expanded');
    }

    toggle.addEventListener('click', function() {
      toggle.classList.toggle('expanded');
      links.classList.toggle('expanded');
    });
  });
});
