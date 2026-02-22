// Inject section nav tabs into top bar
(function() {
  var topBar = document.querySelector('.top-bar');
  if (!topBar) return;

  var path = window.location.pathname;
  var inMechanics = path.indexOf('/mechanics/') !== -1 ||
                    path.indexOf('\\mechanics\\') !== -1 ||
                    path.indexOf('/mechanics/') !== -1;

  // Determine relative paths based on current section
  var moddingHref   = inMechanics ? '../index.html'           : 'index.html';
  var mechanicsHref = inMechanics ? 'index.html'              : 'mechanics/index.html';

  var nav = document.createElement('nav');
  nav.className = 'section-nav';
  nav.innerHTML =
    '<a href="' + moddingHref   + '" class="section-tab' + (inMechanics ? '' : ' active') + '">Modding Reference</a>' +
    '<a href="' + mechanicsHref + '" class="section-tab' + (inMechanics ? ' active' : '') + '">Game Mechanics</a>';

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
