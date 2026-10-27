(() => {
  "use strict";

  const header = document.querySelector(".site-header");
  const nav = document.querySelector("#primaryNav");
  const navToggle = document.querySelector("#navToggle");

  const onScroll = () => {
    if (header) header.classList.toggle("is-scrolled", window.scrollY > 18);
  };
  onScroll();
  window.addEventListener("scroll", onScroll, { passive: true });

  if (navToggle && nav) {
    navToggle.addEventListener("click", () => {
      const open = nav.classList.toggle("is-open");
      navToggle.setAttribute("aria-expanded", String(open));
    });

    nav.querySelectorAll("a").forEach((link) => {
      link.addEventListener("click", () => {
        nav.classList.remove("is-open");
        navToggle.setAttribute("aria-expanded", "false");
      });
    });
  }

  // Active section marker.
  const navLinks = [...document.querySelectorAll(".primary-nav a")];
  const sections = navLinks
    .map((link) => document.querySelector(link.getAttribute("href")))
    .filter(Boolean);

  if ("IntersectionObserver" in window && sections.length) {
    const activeObserver = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
        if (!visible) return;
        navLinks.forEach((link) => {
          link.classList.toggle(
            "is-active",
            link.getAttribute("href") === `#${visible.target.id}`
          );
        });
      },
      { rootMargin: "-20% 0px -64% 0px", threshold: [0.05, 0.25, 0.55] }
    );

    sections.forEach((section) => activeObserver.observe(section));
  }

  // Reveal without requiring markup changes on every card.
  const revealTargets = document.querySelectorAll(
    ".section-heading, .brief-card, .built-card, .machine-step, .module-card, .planned-card, .timeline-item, .download-copy, .download-links"
  );
  if ("IntersectionObserver" in window && !window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
    revealTargets.forEach((el, index) => {
      el.dataset.reveal = "";
      el.style.transitionDelay = `${Math.min(index % 6, 5) * 55}ms`;
    });
    const revealObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add("is-visible");
          revealObserver.unobserve(entry.target);
        });
      },
      { rootMargin: "0px 0px -8% 0px", threshold: 0.06 }
    );
    revealTargets.forEach((el) => revealObserver.observe(el));
  }

  // Counters.
  document.querySelectorAll("[data-count]").forEach((node) => {
    const target = Number(node.dataset.count || 0);
    let started = false;
    const start = () => {
      if (started) return;
      started = true;
      const duration = 900;
      const begin = performance.now();
      const tick = (now) => {
        const p = Math.min(1, (now - begin) / duration);
        const eased = 1 - Math.pow(1 - p, 3);
        node.textContent = String(Math.round(target * eased));
        if (p < 1) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    };
    if ("IntersectionObserver" in window) {
      const observer = new IntersectionObserver((entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          start();
          observer.disconnect();
        }
      }, { threshold: 0.4 });
      observer.observe(node);
    } else {
      node.textContent = String(target);
    }
  });

  // Module filters.
  const filterButtons = [...document.querySelectorAll(".filter-btn")];
  const moduleCards = [...document.querySelectorAll(".module-card")];
  const empty = document.querySelector("#filterEmpty");

  filterButtons.forEach((button) => {
    button.addEventListener("click", () => {
      filterButtons.forEach((item) => item.classList.remove("is-active"));
      button.classList.add("is-active");

      const filter = button.dataset.filter || "all";
      let shown = 0;
      moduleCards.forEach((card) => {
        const match = filter === "all" || card.dataset.state === filter;
        card.classList.toggle("is-hidden", !match);
        if (match) shown += 1;
      });
      if (empty) empty.hidden = shown !== 0;
    });
  });

  // Ambient perspective field. Kept deliberately lightweight.
  const canvas = document.querySelector("#fieldCanvas");
  if (canvas && !window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
    const ctx = canvas.getContext("2d");
    let width = 0;
    let height = 0;
    let dpr = 1;
    let last = 0;
    let raf = 0;

    const resize = () => {
      dpr = Math.min(window.devicePixelRatio || 1, 1.5);
      width = window.innerWidth;
      height = window.innerHeight;
      canvas.width = Math.round(width * dpr);
      canvas.height = Math.round(height * dpr);
      canvas.style.width = `${width}px`;
      canvas.style.height = `${height}px`;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };

    const draw = (time) => {
      if (time - last < 33) {
        raf = requestAnimationFrame(draw);
        return;
      }
      last = time;
      ctx.clearRect(0, 0, width, height);

      const horizon = height * 0.43;
      const vanishX = width * 0.56;
      ctx.lineWidth = 1;

      // Perspective vertical rails.
      for (let i = -12; i <= 12; i += 1) {
        const bottomX = width / 2 + i * (width / 13);
        const topX = vanishX + i * (width / 95);
        const grad = ctx.createLinearGradient(0, horizon, 0, height);
        grad.addColorStop(0, "rgba(81,230,255,0.02)");
        grad.addColorStop(0.65, "rgba(81,230,255,0.075)");
        grad.addColorStop(1, "rgba(81,230,255,0.018)");
        ctx.strokeStyle = grad;
        ctx.beginPath();
        ctx.moveTo(topX, horizon);
        ctx.lineTo(bottomX, height);
        ctx.stroke();
      }

      // Perspective horizontal bands.
      for (let i = 0; i < 15; i += 1) {
        const t = i / 14;
        const y = horizon + Math.pow(t, 2.2) * (height - horizon);
        ctx.strokeStyle = `rgba(126,190,205,${0.018 + t * 0.035})`;
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(width, y);
        ctx.stroke();
      }

      // Slow scan particles.
      const count = Math.max(8, Math.floor(width / 170));
      for (let i = 0; i < count; i += 1) {
        const phase = (time * 0.000012 * (i + 3) + i * 0.137) % 1;
        const y = horizon + phase * (height - horizon);
        const x = ((i * 197 + time * 0.012) % (width + 120)) - 60;
        const alpha = 0.08 + (1 - phase) * 0.2;
        ctx.fillStyle = `rgba(185,255,106,${alpha})`;
        ctx.fillRect(x, y, 1.4, 1.4);
      }

      raf = requestAnimationFrame(draw);
    };

    resize();
    window.addEventListener("resize", resize, { passive: true });
    raf = requestAnimationFrame(draw);
    window.addEventListener("pagehide", () => cancelAnimationFrame(raf), { once: true });
  }

  // Slight parallax on the hero frame only when a mouse exists.
  const heroFrame = document.querySelector(".hero-frame");
  const hero = document.querySelector(".hero");
  if (heroFrame && hero && window.matchMedia("(pointer: fine)").matches &&
      !window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
    hero.addEventListener("pointermove", (event) => {
      const rect = hero.getBoundingClientRect();
      const x = (event.clientX - rect.left) / rect.width - 0.5;
      const y = (event.clientY - rect.top) / rect.height - 0.5;
      heroFrame.style.transform =
        `perspective(1400px) rotateY(${-6 + x * 2.2}deg) rotateX(${2 - y * 1.4}deg) translateY(${-y * 3}px)`;
    });
    hero.addEventListener("pointerleave", () => {
      heroFrame.style.transform = "perspective(1400px) rotateY(-6deg) rotateX(2deg)";
    });
  }
})();
