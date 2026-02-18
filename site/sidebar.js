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
